using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class PromotionService(IUnitOfWork unitOfWork) : IPromotionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<PromotionResponseDto> CreateAsync(PromotionRequest req)
        {
            // Logic kiểm tra thời gian
            if (req.StartTime >= req.EndTime)
            {
                throw new Exception("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
            }

            var repo = _unitOfWork.GetRepository<Promotion>();
            var existing = await repo.FindAsync(p => p.Code == req.Code && p.Isdeleted != true);
            if (existing.Any()) throw new Exception($"Mã '{req.Code}' đã tồn tại.");

            var entity = new Promotion
            {
                Code = req.Code.ToUpper(),
                MinPriceToApply = req.MinPriceToApply,
                Discountvalue = req.DiscountValue,
                MaxDiscountPrice = req.MaxDiscountPrice,
                IsPercentage = req.IsPercentage,
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                IsLimited = req.IsLimited,
                LimitedCount = req.LimitedCount,
                UsedCount = 0,
                Isdeleted = false
            };

            await repo.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            return MapToResponseDto(entity);
        }

        public async Task<IEnumerable<PromotionResponseDto>> GetAllAsync()
        {
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var entities = await promoRepo.FindAsync(p => p.Isdeleted != true);
            return entities.Select(MapToResponseDto);
        }


        public async Task<IEnumerable<PromotionResponseDto>> GetAllAsync(bool isLimited)
        {
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var entities = await promoRepo.FindAsync(p => p.Isdeleted != true && p.IsLimited.Equals(isLimited));
            return entities.Select(MapToResponseDto);
        }

        public async Task DeleteAsync(int id)
        {
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var entity = await promoRepo.GetByIdAsync(id) ?? throw new Exception("Không tìm thấy mã giảm giá.");

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

            return MapToResponseDto(promo);
        }

        public async Task UpdateAsync(int id, PromotionRequest req)
        {
            var repo = _unitOfWork.GetRepository<Promotion>();
            var promo = await repo.GetByIdAsync(id);

            if (promo == null || promo.Isdeleted == true)
                throw new Exception("Không tìm thấy mã giảm giá.");

            // Cập nhật các trường mới
            promo.Code = req.Code.ToUpper();
            promo.MinPriceToApply = req.MinPriceToApply;
            promo.Discountvalue = req.DiscountValue;
            promo.MaxDiscountPrice = req.MaxDiscountPrice;
            promo.IsPercentage = req.IsPercentage;
            promo.StartTime = req.StartTime;
            promo.EndTime = req.EndTime;
            promo.IsLimited = req.IsLimited;
            promo.LimitedCount = req.LimitedCount;

            await repo.UpdateAsync(promo);
            await _unitOfWork.SaveAsync();
        }

        public async Task<PromotionResponseDto> GetByCodeAsync(string code)
        {
            var repo = _unitOfWork.GetRepository<Promotion>();
            var promos = await repo.FindAsync(p => p.Code.Equals(code));
            if (!promos.Any()) throw new Exception("Không tìm thấy mã giảm giá.");

            var promo = promos.FirstOrDefault();

            if (promo == null || promo.Isdeleted == true)
                throw new Exception("Không tìm thấy mã giảm giá.");

            return MapToResponseDto(promo);
        }
        // ----------------

        private PromotionResponseDto MapToResponseDto(Promotion e)
        {
            var now = DateTime.Now;
            return new PromotionResponseDto
            {
                PromotionId = e.Promotionid,
                Code = e.Code ?? "",
                MinPriceToApply = e.MinPriceToApply,
                DiscountValue = e.Discountvalue ?? 0,
                MaxDiscountPrice = e.MaxDiscountPrice,
                IsPercentage = e.IsPercentage ?? false,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                IsLimited = e.IsLimited ?? false,
                LimitedCount = e.LimitedCount,
                IsActive = (e.StartTime <= now && e.EndTime >= now)
            };
        }

    }
}