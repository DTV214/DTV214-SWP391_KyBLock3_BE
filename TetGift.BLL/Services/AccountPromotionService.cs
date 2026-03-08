using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class AccountPromotionService(IUnitOfWork unitOfWork) : IAccountPromotionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        // Cấp mã giảm giá cho một tài khoản cụ thể
        public async Task SaveToAccountAsync(AssignPromotionRequest req)
        {
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var accountRepo = _unitOfWork.GetRepository<Account>();
            var accPromoRepo = _unitOfWork.GetRepository<AccountPromotion>();

            // 0. Kiểm tra account
            var account = accountRepo.GetByIdAsync(req.AccountId);
            if (account.Result == null || !account.Result.Status.Equals(AccountStatus.ACTIVE.ToString()))
                throw new Exception("Cần tài khoản hợp lệ để nhận mã giảm giá.");

            // 1. Kiểm tra Promotion có tồn tại và hợp lệ không
            var promo = await promoRepo.GetByIdAsync(req.PromotionId)
                ?? throw new Exception("Mã giảm giá không tồn tại.");
            if (!promo.IsValid() && !promo.StillNotYet())
                throw new Exception("Mã giảm giá không hợp lệ hoặc đã hết.");

            // 2. Kiểm tra xem tài khoản đã được gán mã này chưa
            var existing = await accPromoRepo.FindAsync(ap =>
                    ap.AccountId == req.AccountId && ap.PromotionId == req.PromotionId);

            if (existing.Any())
            {
                throw new Exception("Mã đã được lưu.");
            }

            var newAccPromo = new AccountPromotion
            {
                AccountId = req.AccountId,
                PromotionId = req.PromotionId,
                Quantity = req.Quantity,
                UsedQuantity = req.UsedQuantity,
                Account = account.Result,
                Promotion = promo

            };
            await accPromoRepo.AddAsync(newAccPromo);
            await _unitOfWork.SaveAsync();
        }

        // Logic đánh dấu đã sử dụng 1 lần mã giảm giá
        public async Task<bool> UsePromotionAsync(int accountId, int promotionId)
        {
            var accountPromoRepo = _unitOfWork.GetRepository<AccountPromotion>();
            var promoRepo = _unitOfWork.GetRepository<Promotion>();
            var records = await accountPromoRepo.FindAsync(ap =>
                ap.AccountId == accountId && ap.PromotionId == promotionId);

            var accPromo = records.FirstOrDefault();
            if (accPromo == null || accPromo.IsUsed())
                return false;

            accPromo.UsedQuantity++;
            await accountPromoRepo.UpdateAsync(accPromo);
            await _unitOfWork.SaveAsync();
            return true;
        }
    }
}