using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork _unitOfWork;
        public PromotionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PromotionResponseDto> CreateAsync(CreatePromotionRequest req)
        {
            if(req.ExpiryDate <= DateTime.UtcNow)
            {
                throw new Exception("Ngày hết hạn phải lớn hơn thời điểm hiện tại.");
            }
            var promoRepo = _unitOfWork.GetRepository<Promotion>();

            var existing = await promoRepo.FindAsync(p => p.Code == req.Code && p.Isdeleted != true);
            if (existing.Any())
            {
                throw new Exception($"Mã giảm giá '{req.Code}' đã tồn tại.");
            }
            //2 map DTO to Entity
            var entity = new Promotion
            {
                Code = req.Code.ToUpper(),
                Discountvalue = req.DiscountValue,
                Expirydate = req.ExpiryDate,
                Isdeleted = false

            };
            //3 Save to DB
            await promoRepo.AddAsync(entity);
            await _unitOfWork.SaveAsync();
            //4 Return DTO
            return new PromotionResponseDto
            {
                PromotionId = entity.Promotionid,
                Code = entity.Code,
                DiscountValue = entity.Discountvalue ?? 0,
                ExpiryDate = entity.Expirydate ?? DateTime.MinValue,
                IsActive = true
            };


        }
        public async Task<IEnumerable<PromotionResponseDto>> GetAllAsync()
        {
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var entities = await promoRepo.FindAsync(p => p.Isdeleted != true);
            return entities.Select(e => new PromotionResponseDto
            {
                PromotionId = e.Promotionid,
                Code = e.Code ?? "",
                DiscountValue = e.Discountvalue ?? 0,
                ExpiryDate = e.Expirydate ?? DateTime.MinValue,
                IsActive = e.Expirydate > DateTime.Now
            });
        }
        public async Task DeleteAsync(int id)
        {
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var entity = await promoRepo.GetByIdAsync(id);

            if (entity == null) throw new Exception("Không tìm thấy mã giảm giá.");

            // Soft Delete
            entity.Isdeleted = true;
            await promoRepo.UpdateAsync(entity);
            // SaveAsync đã được gọi bên trong UpdateAsync nếu repo implement như vậy, 
            // nhưng theo GenericRepository của bạn thì UpdateAsync có gọi SaveChangesAsync.
            // Tuy nhiên hàm UpdateAsync trong GenericRepo mẫu bạn gửi có gọi SaveChangesAsync.
            // Nếu dùng _unitOfWork thì thường ta gọi _unitOfWork.SaveAsync() cuối cùng cho chắc.
        }
        public async Task<PromotionResponseDto> GetByIdAsync(int id)
        {
            var promo = await _unitOfWork.GetRepository<Promotion>().GetByIdAsync(id);
            if (promo == null || promo.Isdeleted == true)
                throw new Exception("Không tìm thấy mã giảm giá.");

            return new PromotionResponseDto
            {
                PromotionId = promo.Promotionid,
                Code = promo.Code ?? "",
                DiscountValue = promo.Discountvalue ?? 0,
                ExpiryDate = promo.Expirydate ?? DateTime.MinValue,
                IsActive = promo.Expirydate > DateTime.Now
            };
        }

        public async Task UpdateAsync(int id, UpdatePromotionRequest req)
        {
            var repo = _unitOfWork.GetRepository<Promotion>();
            var promo = await repo.GetByIdAsync(id);

            if (promo == null || promo.Isdeleted == true)
                throw new Exception("Không tìm thấy mã giảm giá để cập nhật.");

            // 1. LOGIC CHECK TRÙNG (Quan trọng)
            // Tìm xem có thằng nào KHÁC (p.Promotionid != id) mà có Code trùng với Code mới nhập không
            var duplicate = await repo.FindAsync(p =>
                p.Code == req.Code &&
                p.Promotionid != id && // Trừ chính nó ra
                p.Isdeleted != true);

            if (duplicate.Any())
            {
                throw new Exception($"Mã giảm giá '{req.Code}' đã được sử dụng bởi chương trình khác.");
            }

            if (req.ExpiryDate <= DateTime.Now)
            {
                throw new Exception("Ngày hết hạn phải lớn hơn hiện tại.");
            }

            // 2. Update Data
            promo.Code = req.Code.ToUpper();
            promo.Discountvalue = req.DiscountValue;
            promo.Expirydate = req.ExpiryDate;

            // 3. Save
            await repo.UpdateAsync(promo);
            // _unitOfWork.SaveAsync(); // Nếu GenericRepo chưa save thì bỏ comment dòng này
        }
        // ----------------
    }
}