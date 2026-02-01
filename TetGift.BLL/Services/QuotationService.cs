using System.Text.Json;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Common.Enums;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IOrderFromQuotationService _orderSvc;

        public QuotationService(IUnitOfWork uow, IOrderFromQuotationService orderSvc)
        {
            _uow = uow;
            _orderSvc = orderSvc;
        }

        // =========================
        // Helpers
        // =========================
        private static void Ensure(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private async Task<Quotation> GetQuotationOrThrowAsync(int quotationId)
        {
            var qRepo = _uow.GetRepository<Quotation>();
            var q = (await qRepo.FindAsync(x => x.Quotationid == quotationId)).FirstOrDefault();
            if (q == null) throw new Exception("Quotation not found.");
            return q;
        }

        private async Task EnsureQuotationBelongsToAccountAsync(Quotation q, int accountId)
        {
            Ensure(q.Accountid == accountId, "Forbidden.");
            await Task.CompletedTask;
        }

        private async Task AddMessageAsync(int quotationId, string fromRole, int? fromAccountId, string actionType, string? message, object? meta = null, string? toRole = null)
        {
            var msgRepo = _uow.GetRepository<QuotationMessage>();

            var entity = new QuotationMessage
            {
                Quotationid = quotationId,
                Fromrole = fromRole,
                Fromaccountid = fromAccountId,
                Torole = toRole,
                Actiontype = actionType,
                Message = message,
                Metajson = meta == null ? null : JsonSerializer.Serialize(meta),
                Createdat = DateTime.Now
            };

            await msgRepo.AddAsync(entity);
        }

        private async Task UpsertItemsAsync(int quotationId, List<QuotationItemUpsertDto> items)
        {
            var itemRepo = _uow.GetRepository<QuotationItem>();

            // load current items
            var current = (await itemRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();

            // map desired
            var desiredMap = items
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // delete items not in desired
            foreach (var cur in current)
            {
                var pid = cur.Productid ?? 0;
                if (pid == 0 || !desiredMap.ContainsKey(pid))
                {
                    itemRepo.Delete(cur);
                }
            }

            // update existing or add new
            foreach (var kv in desiredMap)
            {
                var pid = kv.Key;
                var qty = kv.Value;

                var existed = current.FirstOrDefault(x => (x.Productid ?? 0) == pid);
                if (existed == null)
                {
                    await itemRepo.AddAsync(new QuotationItem
                    {
                        Quotationid = quotationId,
                        Productid = pid,
                        Quantity = qty,
                        Price = null
                    });
                }
                else
                {
                    existed.Quantity = qty;
                    itemRepo.Update(existed);
                }
            }
        }

        // CUSTOMER - FLOW 1
        public async Task<QuotationSimpleDto> CreateManualAsync(QuotationCreateManualRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");

            var qRepo = _uow.GetRepository<Quotation>();

            var q = new Quotation
            {
                Accountid = req.AccountId,
                Requestdate = DateTime.Now,
                Status = QuotationStatus.DRAFT,
                Quotationtype = QuotationType.MANUAL,
                Desiredbudget = null,
                Desiredpricenote = req.DesiredPriceNote,
                Company = req.Company,
                Address = req.Address,
                Email = req.Email,
                Phone = req.Phone,
                Note = req.Note,
                Revision = 1
            };

            await qRepo.AddAsync(q);
            await _uow.SaveAsync();

            if (req.Items != null && req.Items.Count > 0)
            {
                await UpsertItemsAsync(q.Quotationid, req.Items);
                await _uow.SaveAsync();
            }

            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.NOTE,
                "Created manual quotation draft.");

            await _uow.SaveAsync();

            return new QuotationSimpleDto
            {
                QuotationId = q.Quotationid,
                Status = q.Status,
                QuotationType = q.Quotationtype,
                DesiredBudget = q.Desiredbudget,
                TotalPrice = q.Totalprice,
                Revision = q.Revision
            };
        }

        public async Task<QuotationSimpleDto> UpdateDraftAsync(int quotationId, QuotationUpdateDraftRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            await EnsureQuotationBelongsToAccountAsync(q, req.AccountId);

            Ensure(q.Status == QuotationStatus.DRAFT || q.Status == QuotationStatus.SUBMITTED,
                "Cannot edit quotation at this stage.");

            q.Company = req.Company ?? q.Company;
            q.Address = req.Address ?? q.Address;
            q.Email = req.Email ?? q.Email;
            q.Phone = req.Phone ?? q.Phone;
            q.Note = req.Note ?? q.Note;
            q.Desiredpricenote = req.DesiredPriceNote ?? q.Desiredpricenote;

            var qRepo = _uow.GetRepository<Quotation>();
            qRepo.Update(q);

            if (req.Items != null)
            {
                await UpsertItemsAsync(q.Quotationid, req.Items);
            }

            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.NOTE,
                "Customer updated draft/submitted quotation.");

            await _uow.SaveAsync();

            return new QuotationSimpleDto
            {
                QuotationId = q.Quotationid,
                Status = q.Status,
                QuotationType = q.Quotationtype,
                DesiredBudget = q.Desiredbudget,
                TotalPrice = q.Totalprice,
                Revision = q.Revision
            };
        }

        public async Task SubmitAsync(int quotationId, QuotationSubmitRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            await EnsureQuotationBelongsToAccountAsync(q, req.AccountId);

            Ensure(q.Status == QuotationStatus.DRAFT || q.Status == QuotationStatus.SUBMITTED,
                "Quotation cannot be submitted now.");

            // must have items for manual
            if (string.Equals(q.Quotationtype, QuotationType.MANUAL, StringComparison.OrdinalIgnoreCase))
            {
                var itemRepo = _uow.GetRepository<QuotationItem>();
                var items = (await itemRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();
                Ensure(items.Count > 0, "Quotation items are required.");
            }

            q.Status = QuotationStatus.SUBMITTED;
            q.Submittedat = DateTime.Now;

            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.SUBMIT,
                "Customer submitted quotation request.");

            await _uow.SaveAsync();
        }

        //user accept => create order
        public async Task CustomerAcceptAsync(int quotationId, CustomerDecisionRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            await EnsureQuotationBelongsToAccountAsync(q, req.AccountId);

            Ensure(q.Status == QuotationStatus.WAITING_CUSTOMER, "Quotation is not awaiting customer decision.");

            q.Status = QuotationStatus.CUSTOMER_ACCEPTED;
            q.Customerrespondedat = DateTime.Now;

            _uow.GetRepository<Quotation>().Update(q);
            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.CUSTOMER_ACCEPT,
                req.Message ?? "Customer accepted quotation.");

            await _uow.SaveAsync();

            // Create order via separate service
            var orderId = await _orderSvc.CreateOrderFromQuotationAsync(q.Quotationid, req.AccountId);

            q.Orderid = orderId;
            q.Status = QuotationStatus.CONVERTED_TO_ORDER;

            _uow.GetRepository<Quotation>().Update(q);
            await AddMessageAsync(q.Quotationid, QuotationRole.SYSTEM, null, QuotationAction.CONVERT_ORDER,
                $"Converted to order {orderId}.", meta: new { orderId });

            await _uow.SaveAsync();
        }

        //user reject => back to staff
        public async Task CustomerRejectAsync(int quotationId, CustomerDecisionRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            await EnsureQuotationBelongsToAccountAsync(q, req.AccountId);

            Ensure(q.Status == QuotationStatus.WAITING_CUSTOMER, "Quotation is not awaiting customer decision.");

            q.Status = QuotationStatus.CUSTOMER_REJECTED;
            q.Customerrespondedat = DateTime.Now;
            q.Revision = (q.Revision ?? 1) + 1;

            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.CUSTOMER_REJECT,
                req.Message ?? "Customer rejected quotation.", meta: new { revision = q.Revision }, toRole: QuotationRole.STAFF);

            await _uow.SaveAsync();

            // Optional: auto move back to staff reviewing so staff can rework
            q.Status = QuotationStatus.STAFF_REVIEWING;
            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.SYSTEM, null, QuotationAction.NOTE,
                "Moved back to STAFF_REVIEWING after customer rejected.", toRole: QuotationRole.STAFF);

            await _uow.SaveAsync();
        }

        // STAFF
        public async Task StartReviewAsync(int quotationId, int staffAccountId)
        {
            Ensure(staffAccountId > 0, "StaffAccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);

            Ensure(q.Status == QuotationStatus.SUBMITTED, "Quotation is not in SUBMITTED.");

            q.Status = QuotationStatus.STAFF_REVIEWING;
            q.Staffreviewerid = staffAccountId;
            q.Staffreviewedat = DateTime.Now;

            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.STAFF, staffAccountId, QuotationAction.START_REVIEW,
                "Staff started reviewing quotation.");

            await _uow.SaveAsync();
        }

        //public async Task ProposePriceAsync(int quotationId, StaffProposePriceRequest req)
        //{
        //    Ensure(req.StaffAccountId > 0, "StaffAccountId is required.");
        //    Ensure(req.TotalPrice >= 0, "TotalPrice invalid.");

        //    var q = await GetQuotationOrThrowAsync(quotationId);

        //    Ensure(q.Status == QuotationStatus.STAFF_REVIEWING, "Quotation is not in STAFF_REVIEWING.");

        //    q.Totalprice = req.TotalPrice;
        //    _uow.GetRepository<Quotation>().Update(q);

        //    await AddMessageAsync(q.Quotationid, QuotationRole.STAFF, req.StaffAccountId, QuotationAction.STAFF_PROPOSE,
        //        req.Message ?? $"Staff proposed price: {req.TotalPrice}.");

        //    await _uow.SaveAsync();
        //}

        public async Task ProposeItemDiscountsAsync(int quotationId, StaffProposeItemDiscountRequest req)
        {
            if (req.StaffAccountId <= 0) throw new Exception("StaffAccountId is required.");
            if (req.Lines == null || req.Lines.Count == 0) throw new Exception("Lines is required.");

            var qRepo = _uow.GetRepository<Quotation>();
            var qiRepo = _uow.GetRepository<QuotationItem>();
            var feeRepo = _uow.GetRepository<QuotationFee>();
            var pRepo = _uow.GetRepository<Product>();

            var q = (await qRepo.FindAsync(x => x.Quotationid == quotationId)).FirstOrDefault();
            if (q == null) throw new Exception("Quotation not found.");
            if (q.Status != QuotationStatus.STAFF_REVIEWING)
                throw new Exception("Quotation is not in STAFF_REVIEWING.");

            decimal quotationTotalAfterDiscount = 0;

            foreach (var line in req.Lines)
            {
                if (line.QuotationItemId <= 0) continue;
                if (line.DiscountPercent < 0 || line.DiscountPercent > 100)
                    throw new Exception("DiscountPercent must be between 0 and 100.");

                var qi = (await qiRepo.FindAsync(x => x.Quotationitemid == line.QuotationItemId)).FirstOrDefault();
                if (qi == null) throw new Exception($"QuotationItem not found: {line.QuotationItemId}");
                if (qi.Quotationid != quotationId) throw new Exception("QuotationItem does not belong to this quotation.");

                var pid = qi.Productid ?? 0;
                var qty = qi.Quantity ?? 0;
                if (pid <= 0 || qty <= 0) throw new Exception("QuotationItem missing product/quantity.");

                var product = (await pRepo.FindAsync(x => x.Productid == pid)).FirstOrDefault();
                if (product == null) throw new Exception($"Product not found: {pid}");

                var unitPrice = product.Price ?? 0;
                if (unitPrice <= 0) throw new Exception($"Invalid product price: {pid}");

                //quootationprice tính tổng gốc trước giảm
                var originalLineTotal = unitPrice * qty;
                qi.Price = Math.Round(originalLineTotal, 2);
                qiRepo.Update(qi);

                //quotationfee tính tổng sau giảm
                var afterDiscount = originalLineTotal * (1 - (line.DiscountPercent / 100m));
                afterDiscount = Math.Round(afterDiscount, 2);

                // Upsert 1 fee record for this item (take latest if exists)
                var existingFees = (await feeRepo.FindAsync(f => f.Quotationitemid == qi.Quotationitemid)).ToList();
                var fee = existingFees.OrderByDescending(f => f.Quotationfeeid).FirstOrDefault();

                if (fee == null)
                {
                    fee = new QuotationFee
                    {
                        Quotationitemid = qi.Quotationitemid,
                        Issubtracted = 0,
                        Description = $"{line.DiscountPercent}%",
                        Price = afterDiscount
                    };
                    await feeRepo.AddAsync(fee);
                }
                else
                {
                    fee.Issubtracted = 0;
                    fee.Description = $"{line.DiscountPercent}%";
                    fee.Price = afterDiscount;
                    feeRepo.Update(fee);
                }

                quotationTotalAfterDiscount += afterDiscount;
            }

            // Update tổng quotation
            q.Totalprice = Math.Round(quotationTotalAfterDiscount, 2);
            q.Staffreviewerid = req.StaffAccountId;
            q.Staffreviewedat = DateTime.Now;
            qRepo.Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.STAFF, req.StaffAccountId, QuotationAction.STAFF_PROPOSE,
                req.Message ?? "Staff updated item discounts and totals.",
                meta: new { totalAfterDiscount = q.Totalprice });

            await _uow.SaveAsync();
        }

        public async Task SendToAdminAsync(int quotationId, int staffAccountId, string? message)
        {
            Ensure(staffAccountId > 0, "StaffAccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            Ensure(q.Status == QuotationStatus.STAFF_REVIEWING, "Quotation is not in STAFF_REVIEWING.");

            var feeRepo = _uow.GetRepository<QuotationFee>();
            var qiRepo = _uow.GetRepository<QuotationItem>();

            var items = (await qiRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();
            Ensure(items.Count > 0, "Quotation must have items.");

            foreach (var it in items)
            {
                var hasFee = (await feeRepo.FindAsync(f => f.Quotationitemid == it.Quotationitemid)).Any();
                Ensure(hasFee, "Each quotation item must have a fee (discount result) before sending to admin.");
            }

            Ensure(q.Totalprice != null && q.Totalprice > 0, "Total price after discount must be set.");


            q.Status = QuotationStatus.WAITING_ADMIN;
            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.STAFF, staffAccountId, QuotationAction.SEND_ADMIN,
                message ?? "Sent to admin for approval.", toRole: QuotationRole.ADMIN);

            await _uow.SaveAsync();
        }

        // ADMIN
        public async Task AdminApproveAsync(int quotationId, AdminDecisionRequest req)
        {
            Ensure(req.AdminAccountId > 0, "AdminAccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            Ensure(q.Status == QuotationStatus.WAITING_ADMIN, "Quotation is not in WAITING_ADMIN.");

            q.Status = QuotationStatus.WAITING_CUSTOMER;
            q.Adminreviewerid = req.AdminAccountId;
            q.Adminreviewedat = DateTime.Now;

            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.ADMIN, req.AdminAccountId, QuotationAction.ADMIN_APPROVE,
                req.Message ?? "Admin approved. Waiting customer confirmation.", toRole: QuotationRole.CUSTOMER);

            // TODO: send email to customer if you want (reuse your email sender)
            await _uow.SaveAsync();
        }

        public async Task AdminRejectAsync(int quotationId, AdminDecisionRequest req)
        {
            Ensure(req.AdminAccountId > 0, "AdminAccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            Ensure(q.Status == QuotationStatus.WAITING_ADMIN, "Quotation is not in WAITING_ADMIN.");

            q.Status = QuotationStatus.ADMIN_REJECTED;
            q.Adminreviewerid = req.AdminAccountId;
            q.Adminreviewedat = DateTime.Now;

            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.ADMIN, req.AdminAccountId, QuotationAction.ADMIN_REJECT,
                req.Message ?? "Admin rejected. Please revise proposal.", toRole: QuotationRole.STAFF);

            await _uow.SaveAsync();

            // Optional: auto move back to STAFF_REVIEWING
            q.Status = QuotationStatus.STAFF_REVIEWING;
            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.SYSTEM, null, QuotationAction.NOTE,
                "Moved back to STAFF_REVIEWING after admin rejected.", toRole: QuotationRole.STAFF);

            await _uow.SaveAsync();
        }

        // LIST
        public async Task<List<QuotationListItemDto>> GetCustomerQuotationsAsync(int accountId, string? status = null)
        {
            if (accountId <= 0) throw new Exception("AccountId is required.");

            var repo = _uow.GetRepository<Quotation>();
            var data = (await repo.FindAsync(q =>
                    q.Accountid == accountId
                    && (status == null || q.Status == status)
                ))
                .OrderByDescending(x => x.Requestdate)
                .Select(x => new QuotationListItemDto
                {
                    QuotationId = x.Quotationid,
                    Status = x.Status,
                    RequestDate = x.Requestdate,
                    Company = x.Company,
                    TotalPrice = x.Totalprice,
                    Revision = x.Revision
                })
                .ToList();

            return data;
        }

        public async Task<List<QuotationListItemDto>> GetStaffQuotationsAsync(string? status = null)
        {
            var repo = _uow.GetRepository<Quotation>();
            var data = (await repo.FindAsync(q =>
                    (status == null || q.Status == status)
                ))
                .OrderByDescending(x => x.Requestdate)
                .Select(x => new QuotationListItemDto
                {
                    QuotationId = x.Quotationid,
                    Status = x.Status,
                    RequestDate = x.Requestdate,
                    Company = x.Company,
                    TotalPrice = x.Totalprice,
                    Revision = x.Revision
                })
                .ToList();

            return data;
        }

        public async Task<List<QuotationListItemDto>> GetAdminQuotationsAsync(string? status = null)
        {
            var repo = _uow.GetRepository<Quotation>();
            var data = (await repo.FindAsync(q =>
                    (status == null || q.Status == status)
                ))
                .OrderByDescending(x => x.Requestdate)
                .Select(x => new QuotationListItemDto
                {
                    QuotationId = x.Quotationid,
                    Status = x.Status,
                    RequestDate = x.Requestdate,
                    Company = x.Company,
                    TotalPrice = x.Totalprice,
                    Revision = x.Revision
                })
                .ToList();

            return data;
        }

        private static decimal ParsePercent(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            s = s.Trim();
            if (s.EndsWith("%")) s = s.Substring(0, s.Length - 1);
            return decimal.TryParse(s, out var v) ? v : 0m;
        }

        private async Task<QuotationDetailDto> BuildQuotationDetailAsync(int quotationId)
        {
            var qRepo = _uow.GetRepository<Quotation>();
            var qiRepo = _uow.GetRepository<QuotationItem>();
            var qfRepo = _uow.GetRepository<QuotationFee>();
            var pRepo = _uow.GetRepository<Product>();
            var msgRepo = _uow.GetRepository<QuotationMessage>();

            var q = (await qRepo.FindAsync(x => x.Quotationid == quotationId)).FirstOrDefault();
            if (q == null) throw new Exception("Quotation not found.");

            var items = (await qiRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();
            var itemIds = items.Select(x => x.Quotationitemid).ToList();
            var productIds = items.Where(x => x.Productid != null).Select(x => x.Productid!.Value).Distinct().ToList();

            //products
            var products = productIds.Count == 0
                ? new List<Product>()
                : (await pRepo.FindAsync(p => productIds.Contains(p.Productid))).ToList();

            //fees
            var fees = itemIds.Count == 0
                ? new List<QuotationFee>()
                : (await qfRepo.FindAsync(f => f.Quotationitemid != null && itemIds.Contains(f.Quotationitemid.Value))).ToList();

            var latestFeeByItemId = fees
                .Where(f => f.Quotationitemid != null)
                .GroupBy(f => f.Quotationitemid!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Quotationfeeid).First());

            // messages
            var messages = (await msgRepo.FindAsync(m => m.Quotationid == quotationId))
                .OrderBy(x => x.Createdat)
                .Select(x => new QuotationMessageDto
                {
                    QuotationMessageId = x.Quotationmessageid,
                    FromRole = x.Fromrole,
                    FromAccountId = x.Fromaccountid,
                    ToRole = x.Torole,
                    ActionType = x.Actiontype,
                    Message = x.Message,
                    MetaJson = x.Metajson,
                    CreatedAt = x.Createdat
                })
                .ToList();

            // build lines + totals
            decimal totalOriginal = 0;
            decimal totalAfter = 0;

            var lines = new List<QuotationLineDto>();

            foreach (var it in items)
            {
                var pid = it.Productid ?? 0;
                var qty = it.Quantity ?? 0;
                if (pid <= 0 || qty <= 0) continue;

                var prod = products.FirstOrDefault(p => p.Productid == pid);
                var unit = prod?.Price ?? 0m;

                // original line total: QuotationItem.Price (you define as line total)
                var originalLineTotal = it.Price ?? (unit * qty);

                // discounted line total: from fee.Price (you define as line total after discount)
                latestFeeByItemId.TryGetValue(it.Quotationitemid, out var fee);
                var after = fee?.Price ?? originalLineTotal;

                // discount percent
                var percent = ParsePercent(fee?.Description);

                totalOriginal += originalLineTotal;
                totalAfter += after;

                lines.Add(new QuotationLineDto
                {
                    QuotationItemId = it.Quotationitemid,
                    ProductId = pid,
                    Sku = prod?.Sku,
                    ProductName = prod?.Productname,
                    Quantity = qty,
                    UnitPrice = unit,
                    OriginalLineTotal = Math.Round(originalLineTotal, 2),
                    DiscountPercent = percent,
                    AfterDiscountLineTotal = Math.Round(after, 2)
                });
            }

            var dto = new QuotationDetailDto
            {
                QuotationId = q.Quotationid,
                AccountId = q.Accountid,
                OrderId = q.Orderid,
                Status = q.Status,
                QuotationType = q.Quotationtype,
                Revision = q.Revision,

                RequestDate = q.Requestdate,
                SubmittedAt = q.Submittedat,
                StaffReviewedAt = q.Staffreviewedat,
                AdminReviewedAt = q.Adminreviewedat,
                CustomerRespondedAt = q.Customerrespondedat,

                StaffReviewerId = q.Staffreviewerid,
                AdminReviewerId = q.Adminreviewerid,

                Company = q.Company,
                Address = q.Address,
                Email = q.Email,
                Phone = q.Phone,

                DesiredPriceNote = q.Desiredpricenote,
                Note = q.Note,

                TotalOriginal = Math.Round(totalOriginal, 2),
                TotalAfterDiscount = Math.Round(totalAfter, 2),
                TotalDiscountAmount = Math.Round(totalOriginal - totalAfter, 2),

                Lines = lines,
                Messages = messages
            };

            return dto;
        }

        // =========================
        // DETAIL by ROLE
        // =========================
        public async Task<QuotationDetailDto> GetCustomerQuotationDetailAsync(int quotationId, int accountId)
        {
            if (accountId <= 0) throw new Exception("AccountId is required.");

            var qRepo = _uow.GetRepository<Quotation>();
            var q = (await qRepo.FindAsync(x => x.Quotationid == quotationId)).FirstOrDefault();
            if (q == null) throw new Exception("Quotation not found.");
            if (q.Accountid != accountId) throw new Exception("Forbidden.");

            return await BuildQuotationDetailAsync(quotationId);
        }

        public async Task<QuotationDetailDto> GetStaffQuotationDetailAsync(int quotationId)
        {
            return await BuildQuotationDetailAsync(quotationId);
        }

        public async Task<QuotationDetailDto> GetAdminQuotationDetailAsync(int quotationId)
        {
            return await BuildQuotationDetailAsync(quotationId);
        }

        // =========================
        // CUSTOMER - FLOW 2 (RECOMMEND)
        // =========================
        public async Task<RecommendPreviewDto> RequestRecommendAsync(QuotationRecommendRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");
            Ensure(req.Budget > 0, "Budget must be > 0.");
            Ensure(req.Categories != null && req.Categories.Count > 0, "Categories required.");

            var qRepo = _uow.GetRepository<Quotation>();
            var q = new Quotation
            {
                Accountid = req.AccountId,
                Requestdate = DateTime.Now,
                Status = QuotationStatus.DRAFT,
                Quotationtype = QuotationType.BUDGET_RECOMMEND,
                Desiredbudget = req.Budget,
                Note = req.Note,
                Revision = 1
            };

            await qRepo.AddAsync(q);
            await _uow.SaveAsync();

            // save category requests
            var catReqRepo = _uow.GetRepository<QuotationCategoryRequest>();
            foreach (var c in req.Categories)
            {
                await catReqRepo.AddAsync(new QuotationCategoryRequest
                {
                    Quotationid = q.Quotationid,
                    Categoryid = c.CategoryId,
                    Quantity = c.Quantity,
                    Note = c.Note,
                    Createdat = DateTime.Now
                });
            }
            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.RECOMMEND_PREVIEW,
                "Customer requested system recommendation.", meta: new { budget = req.Budget });

            await _uow.SaveAsync();

            // ===== Recommend MVP algorithm (simple) =====
            // Pick products by categories, cheapest first, until reaching budget.
            var productRepo = _uow.GetRepository<Product>();

            var categoryIds = req.Categories.Select(x => x.CategoryId).Distinct().ToList();

            // NOTE: repo.Entities is IQueryable<Product> (DAL has EF). This is okay.
            var candidates = productRepo.Entities
                .Where(p => p.Categoryid != null && categoryIds.Contains(p.Categoryid.Value))
                .Where(p => p.Status == null || p.Status == ProductStatus.ACTIVE) // optional
                .ToList();

            // order by price
            candidates = candidates.OrderBy(p => p.Price ?? decimal.MaxValue).ToList();

            var chosen = new List<RecommendPreviewItemDto>();
            decimal total = 0;

            // If user gave quantities per category, try to satisfy those first
            foreach (var cr in req.Categories)
            {
                if (cr.Quantity == null || cr.Quantity <= 0) continue;

                var picks = candidates.Where(p => (p.Categoryid ?? 0) == cr.CategoryId).ToList();
                if (picks.Count == 0) continue;

                // pick the cheapest product for that category
                var p0 = picks.First();
                var qty = cr.Quantity.Value;
                var line = (p0.Price ?? 0) * qty;

                if (total + line <= req.Budget)
                {
                    chosen.Add(new RecommendPreviewItemDto
                    {
                        ProductId = p0.Productid,
                        ProductName = p0.Productname,
                        Price = p0.Price,
                        Quantity = qty
                    });
                    total += line;
                }
            }

            // Fill remaining budget with cheapest items
            foreach (var p in candidates)
            {
                if (total >= req.Budget) break;
                var price = p.Price ?? 0;
                if (price <= 0) continue;

                // add 1 item each time
                if (total + price <= req.Budget)
                {
                    var existed = chosen.FirstOrDefault(x => x.ProductId == p.Productid);
                    if (existed == null)
                    {
                        chosen.Add(new RecommendPreviewItemDto
                        {
                            ProductId = p.Productid,
                            ProductName = p.Productname,
                            Price = p.Price,
                            Quantity = 1
                        });
                    }
                    else
                    {
                        existed.Quantity += 1;
                    }

                    total += price;
                }
            }

            return new RecommendPreviewDto
            {
                Budget = req.Budget,
                EstimatedTotal = total,
                Items = chosen,
                Quotation = new QuotationSimpleDto
                {
                    QuotationId = q.Quotationid,
                    Status = q.Status,
                    QuotationType = q.Quotationtype,
                    DesiredBudget = q.Desiredbudget,
                    TotalPrice = total,
                    Revision = q.Revision
                }
            };
        }

        public async Task CustomerConfirmRecommendAsync(int quotationId, QuotationRecommendConfirmRequest req)
        {
            Ensure(req.AccountId > 0, "AccountId is required.");

            var q = await GetQuotationOrThrowAsync(quotationId);
            await EnsureQuotationBelongsToAccountAsync(q, req.AccountId);

            Ensure(string.Equals(q.Quotationtype, QuotationType.BUDGET_RECOMMEND, StringComparison.OrdinalIgnoreCase),
                "Not a recommend quotation.");

            Ensure(q.Status == QuotationStatus.DRAFT || q.Status == QuotationStatus.SUBMITTED,
                "Cannot confirm recommend at this stage.");

            // Re-run recommend based on saved category requests
            var catReqRepo = _uow.GetRepository<QuotationCategoryRequest>();
            var catReqs = (await catReqRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();
            Ensure(catReqs.Count > 0, "No category request found.");

            var budget = q.Desiredbudget ?? 0;
            Ensure(budget > 0, "Budget invalid.");

            // Build pseudo request and reuse logic quickly (simple)
            var tempReq = new QuotationRecommendRequest
            {
                AccountId = req.AccountId,
                Budget = budget,
                Note = q.Note,
                Categories = catReqs.Select(x => new RecommendCategoryInputDto
                {
                    CategoryId = x.Categoryid,
                    Quantity = x.Quantity,
                    Note = x.Note
                }).ToList()
            };

            // get preview items
            var preview = await RequestRecommendInternalPreviewAsync(q.Quotationid, tempReq);

            // Convert preview items -> QuotationItem
            await UpsertItemsAsync(q.Quotationid, preview.Items.Select(i => new QuotationItemUpsertDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList());

            q.Totalprice = preview.EstimatedTotal;
            q.Status = QuotationStatus.CUSTOMER_ACCEPTED;
            q.Customerrespondedat = DateTime.Now;

            _uow.GetRepository<Quotation>().Update(q);

            await AddMessageAsync(q.Quotationid, QuotationRole.CUSTOMER, req.AccountId, QuotationAction.RECOMMEND_CONFIRM,
                "Customer confirmed system recommendation.");

            await _uow.SaveAsync();

            if (req.AutoCreateOrder)
            {
                var orderId = await _orderSvc.CreateOrderFromQuotationAsync(q.Quotationid, req.AccountId);
                q.Orderid = orderId;
                q.Status = QuotationStatus.CONVERTED_TO_ORDER;

                _uow.GetRepository<Quotation>().Update(q);
                await AddMessageAsync(q.Quotationid, QuotationRole.SYSTEM, null, QuotationAction.CONVERT_ORDER,
                    $"Converted to order {orderId}.", meta: new { orderId });

                await _uow.SaveAsync();
            }
        }

        // internal helper to avoid creating new quotation inside RequestRecommendAsync
        private async Task<RecommendPreviewDto> RequestRecommendInternalPreviewAsync(int quotationId, QuotationRecommendRequest req)
        {
            var productRepo = _uow.GetRepository<Product>();
            var categoryIds = req.Categories.Select(x => x.CategoryId).Distinct().ToList();

            var candidates = productRepo.Entities
                .Where(p => p.Categoryid != null && categoryIds.Contains(p.Categoryid.Value))
                .Where(p => p.Status == null || p.Status == "Active")
                .ToList()
                .OrderBy(p => p.Price ?? decimal.MaxValue)
                .ToList();

            var chosen = new List<RecommendPreviewItemDto>();
            decimal total = 0;

            foreach (var cr in req.Categories)
            {
                if (cr.Quantity == null || cr.Quantity <= 0) continue;

                var picks = candidates.Where(p => (p.Categoryid ?? 0) == cr.CategoryId).ToList();
                if (picks.Count == 0) continue;

                var p0 = picks.First();
                var qty = cr.Quantity.Value;
                var line = (p0.Price ?? 0) * qty;

                if (total + line <= req.Budget)
                {
                    chosen.Add(new RecommendPreviewItemDto
                    {
                        ProductId = p0.Productid,
                        ProductName = p0.Productname,
                        Price = p0.Price,
                        Quantity = qty
                    });
                    total += line;
                }
            }

            foreach (var p in candidates)
            {
                if (total >= req.Budget) break;
                var price = p.Price ?? 0;
                if (price <= 0) continue;

                if (total + price <= req.Budget)
                {
                    var existed = chosen.FirstOrDefault(x => x.ProductId == p.Productid);
                    if (existed == null)
                    {
                        chosen.Add(new RecommendPreviewItemDto
                        {
                            ProductId = p.Productid,
                            ProductName = p.Productname,
                            Price = p.Price,
                            Quantity = 1
                        });
                    }
                    else existed.Quantity += 1;

                    total += price;
                }
            }

            return new RecommendPreviewDto
            {
                Budget = req.Budget,
                EstimatedTotal = total,
                Items = chosen,
                Quotation = new QuotationSimpleDto
                {
                    QuotationId = quotationId,
                    Status = QuotationStatus.DRAFT,
                    QuotationType = QuotationType.BUDGET_RECOMMEND,
                    DesiredBudget = req.Budget,
                    TotalPrice = total,
                    Revision = 1
                }
            };
        }
    }
}
