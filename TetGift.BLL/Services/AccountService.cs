using CloudinaryDotNet.Core;
using System;
using System.Linq; // <-- Thêm thư viện này để dùng FirstOrDefault()
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

            // Lấy ra danh sách các account khớp điều kiện
            var accounts = await repo.FindAsync(
                predicate: a => a.Accountid == accountId
            );

            // Lấy account đầu tiên (và duy nhất) trong danh sách đó
            var account = accounts.FirstOrDefault();

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
                Status = account.Status
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