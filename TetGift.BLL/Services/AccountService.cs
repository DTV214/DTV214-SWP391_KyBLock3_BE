using Microsoft.EntityFrameworkCore; // Nhớ using cái này để dùng Include
using System;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserProfileDto> GetProfileAsync(int accountId)
        {
            var repo = _unitOfWork.GetRepository<Account>();

            // SỬA ĐỔI: Sử dụng Include để lấy luôn thông tin Wallet
            var account = await repo.FindAsync(
                predicate: a => a.Accountid == accountId,
                include: q => q.Include(a => a.Wallet) // Join với bảng Wallet
            );

            if (account == null)
                throw new Exception("Tài khoản không tồn tại.");

            return new UserProfileDto
            {
                AccountId = account.Accountid,
                Username = account.Username,
                Email = account.Email ?? "",
                FullName = account.Fullname,
                Phone = account.Phone,
                Address = account.Address,
                Role = account.Role,
                Status = account.Status,

                // MAP BALANCE: Nếu chưa có ví thì hiển thị 0
                WalletBalance = account.Wallet?.Balance ?? 0
            };
        }

        public async Task UpdateProfileAsync(int accountId, UpdateProfileRequest req)
        {
            var repo = _unitOfWork.GetRepository<Account>();
            var account = await repo.GetByIdAsync(accountId);

            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            account.Fullname = req.FullName;
            account.Phone = req.Phone;
            account.Address = req.Address;

            await repo.UpdateAsync(account);
        }

        public async Task DeactivateAccountAsync(int accountId)
        {
            var repo = _unitOfWork.GetRepository<Account>();
            var account = await repo.GetByIdAsync(accountId);

            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            account.Status = "DELETED";
            await repo.UpdateAsync(account);
        }
    }
}