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

            // 1) load quotation
            var q = (await qRepo.FindAsync(x => x.Quotationid == quotationId)).FirstOrDefault();
            if (q == null) throw new Exception("Quotation not found.");
            if (q.Accountid != accountId) throw new Exception("Forbidden.");

            // prevent duplicate conversion
            if (q.Orderid != null && q.Orderid > 0)
                return q.Orderid.Value;

            // Only allow conversion at accepted stage
            if (!string.Equals(q.Status, QuotationStatus.CUSTOMER_ACCEPTED, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(q.Status, QuotationStatus.CONVERTED_TO_ORDER, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Quotation is not accepted yet.");
            }

            // 2) load items
            var items = (await qiRepo.FindAsync(x => x.Quotationid == quotationId)).ToList();
            if (items.Count == 0) throw new Exception("Quotation has no items.");

            // 3) create order first
            var order = new Order
            {
                Accountid = accountId,
                Orderdatetime = DateTime.Now,
                Status = "Pending",
                Totalprice = 0,

                Customername = q.Company,
                Customerphone = q.Phone,
                Customeremail = q.Email,
                Customeraddress = q.Address,
                Note = q.Note
            };

            await oRepo.AddAsync(order);
            await _uow.SaveAsync(); // to get Orderid

            // 4) build order details using QuotationFee.Price (after discount)
            decimal total = 0;

            foreach (var it in items)
            {
                var pid = it.Productid ?? 0;
                var qty = it.Quantity ?? 0;
                if (pid <= 0 || qty <= 0) continue;

                // line total BEFORE discount (already stored in quotation_item.price)
                decimal originalLineTotal = it.Price ?? 0;

                // fallback if staff chưa set price gốc cho item
                if (originalLineTotal <= 0)
                {
                    var prod = (await pRepo.FindAsync(x => x.Productid == pid)).FirstOrDefault();
                    if (prod == null) throw new Exception($"Product not found: {pid}");

                    var unitPrice = prod.Price ?? 0;
                    if (unitPrice <= 0) throw new Exception($"Invalid product price for product {pid}.");

                    originalLineTotal = unitPrice * qty;
                    // (optional) update lại item.Price cho đúng rule dữ liệu
                    it.Price = originalLineTotal;
                    qiRepo.Update(it);
                }

                // line total AFTER discount: lấy từ quotation_fee.price (latest)
                var fees = (await qfRepo.FindAsync(f => f.Quotationitemid == it.Quotationitemid)).ToList();
                var fee = fees.OrderByDescending(f => f.Quotationfeeid).FirstOrDefault();

                decimal finalLineTotal = fee?.Price ?? 0;
                if (finalLineTotal <= 0)
                {
                    // fallback: nếu chưa có fee thì dùng giá gốc
                    finalLineTotal = originalLineTotal;
                }

                // IMPORTANT: finalLineTotal là "tổng tiền line", không nhân qty nữa
                total += finalLineTotal;

                await odRepo.AddAsync(new OrderDetail
                {
                    Orderid = order.Orderid,
                    Productid = pid,
                    Quantity = qty,
                    Amount = finalLineTotal // total after discount
                });
            }

            if (total <= 0) throw new Exception("Order total invalid (0).");

            // 5) update order total
            order.Totalprice = total;
            oRepo.Update(order);

            // 6) link quotation -> order
            q.Orderid = order.Orderid;
            q.Status = QuotationStatus.CONVERTED_TO_ORDER;
            qRepo.Update(q);

            await _uow.SaveAsync();

            return order.Orderid;
        }
    }
}
