using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Common.Enums;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class OrderFromQuotationService : IOrderFromQuotationService
    {
        private readonly IUnitOfWork _uow;

        public OrderFromQuotationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<int> CreateOrderFromQuotationAsync(int quotationId, int accountId)
        {
            if (quotationId <= 0) throw new Exception("quotationId is required.");
            if (accountId <= 0) throw new Exception("accountId is required.");

            var qRepo = _uow.GetRepository<Quotation>();
            var qiRepo = _uow.GetRepository<QuotationItem>();
            var qfRepo = _uow.GetRepository<QuotationFee>();
            var pRepo = _uow.GetRepository<Product>();

            var oRepo = _uow.GetRepository<Order>();
            var odRepo = _uow.GetRepository<OrderDetail>();

            var q = (await qRepo.FindAsync(x => x.Quotationid == quotationId)).FirstOrDefault();
            if (q == null) throw new Exception("Quotation not found.");
            if ((q.Accountid ?? 0) != accountId) throw new Exception("Forbidden.");

            // chống double convert
            if (q.Orderid != null && q.Orderid > 0) return q.Orderid.Value;

            var items = (await qiRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();
            if (items.Count == 0) throw new Exception("Quotation has no items.");

            _uow.BeginTransaction();
            try
            {
                // 1) ensure QuotationItem.Price = line total gốc (snapshot)
                foreach (var it in items)
                {
                    var pid = it.Productid ?? 0;
                    var qty = it.Quantity ?? 0;
                    if (pid <= 0 || qty <= 0)
                        throw new Exception($"QuotationItem {it.Quotationitemid} missing product/quantity.");

                    if (it.Price == null || it.Price <= 0)
                    {
                        var prod = (await pRepo.FindAsync(p => p.Productid == pid)).FirstOrDefault()
                                   ?? throw new Exception($"Product not found: {pid}");

                        var unit = prod.Price ?? 0m;
                        if (unit <= 0) throw new Exception($"Invalid product price: {pid}");

                        it.Price = Math.Round(unit * qty, 2);
                        qiRepo.Update(it);
                    }
                }
                await _uow.SaveAsync();

                // 2) load all fees (batch)
                var itemIds = items.Select(i => i.Quotationitemid).ToList();
                var fees = itemIds.Count == 0
                    ? new List<QuotationFee>()
                    : (await qfRepo.FindAsync(f => f.Quotationitemid != null && itemIds.Contains(f.Quotationitemid.Value))).ToList();

                var feesByItem = fees
                    .Where(f => f.Quotationitemid != null)
                    .GroupBy(f => f.Quotationitemid!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 3) create Order (PENDING: chờ payment)
                var order = new Order
                {
                    Accountid = accountId,
                    Orderdatetime = DateTime.Now,
                    Status = OrderStatus.PENDING,

                    Customername = q.Company,
                    Customerphone = q.Phone,
                    Customeremail = q.Email,
                    Customeraddress = q.Address,
                    Note = q.Note,

                    Totalprice = 0m
                };

                await oRepo.AddAsync(order);
                await _uow.SaveAsync();

                // 4) create OrderDetails + compute total = sum(original) - sum(sub) + sum(add)
                decimal total = 0m;

                foreach (var it in items)
                {
                    var pid = it.Productid ?? 0;
                    var qty = it.Quantity ?? 0;
                    if (pid <= 0 || qty <= 0) continue;

                    var original = Math.Round(it.Price ?? 0m, 2);

                    feesByItem.TryGetValue(it.Quotationitemid, out var itemFees);
                    itemFees ??= new List<QuotationFee>();

                    var sub = itemFees.Where(f => (f.Issubtracted ?? 0) == 0).Sum(f => f.Price ?? 0m);
                    var add = itemFees.Where(f => (f.Issubtracted ?? 0) == 1).Sum(f => f.Price ?? 0m);

                    sub = Math.Round(sub, 2);
                    add = Math.Round(add, 2);

                    var finalLine = Math.Round(original - sub + add, 2);
                    if (finalLine < 0) throw new Exception($"Final line total cannot be negative (quotationItemId={it.Quotationitemid}).");

                    total += finalLine;

                    await odRepo.AddAsync(new OrderDetail
                    {
                        Orderid = order.Orderid,
                        Productid = pid,
                        Quantity = qty,
                        Amount = finalLine
                    });
                }

                if (total <= 0) throw new Exception("Order total invalid.");

                order.Totalprice = Math.Round(total, 2);
                oRepo.Update(order);

                // 5) link quotation -> order
                q.Orderid = order.Orderid;
                q.Status = QuotationStatus.CONVERTED_TO_ORDER;
                qRepo.Update(q);

                await _uow.SaveAsync();
                _uow.CommitTransaction();

                return order.Orderid;
            }
            catch
            {
                _uow.RollBack();
                throw;
            }
        }

    }
}
