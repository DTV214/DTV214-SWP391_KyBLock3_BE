using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TetGift.BLL.Common.Constraint;
// using TetGift.BLL.Common.Securities; // Uncomment dòng này nếu bạn dùng PasswordHasher
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class AdminAccountService : IAdminAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminAccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AccountAdminDto>> GetAllAccountsAsync()
        {
            var repo = _unitOfWork.GetRepository<Account>();
            var accounts = await repo.GetAllAsync();

            return accounts.Select(a => new AccountAdminDto
            {
                AccountId = a.Accountid,
                Username = a.Username,
                Email = a.Email,
                Fullname = a.Fullname,
                Phone = a.Phone,
                Address = a.Address,
                Role = a.Role,
                Status = a.Status
            }).OrderByDescending(a => a.AccountId);
        }

        public async Task<AccountAdminDto> GetAccountByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Account>();
            var account = await repo.GetByIdAsync(id);

            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            return new AccountAdminDto
            {
                AccountId = account.Accountid,
                Username = account.Username,
                Email = account.Email,
                Fullname = account.Fullname,
                Phone = account.Phone,
                Address = account.Address,
                Role = account.Role,
                Status = account.Status
            };
        }

        public async Task<AccountAdminDto> CreateAccountAsync(CreateAccountAdminRequest req)
        {
            var repo = _unitOfWork.GetRepository<Account>();

            // Kiểm tra trùng lặp Username
            var existing = await repo.FindAsync(a => a.Username == req.Username);
            if (existing.Any()) throw new Exception("Username đã tồn tại trên hệ thống.");

            var account = new Account
            {
                Username = req.Username,
                // TODO: Gọi hàm Hash Password từ Helper của bạn. Ví dụ:
                // Password = PasswordHasher.Hash(req.Password),
                Password = req.Password, // Tạm gán cứng, bạn nhớ đổi thành hash nhé
                Email = req.Email,
                Fullname = req.Fullname,
                Phone = req.Phone,
                Role = req.Role,
                Status = AccountStatus.ACTIVE // Mặc định tạo ra là ACTIVE
            };

            await repo.AddAsync(account);
            await _unitOfWork.SaveAsync();

            return new AccountAdminDto
            {
                AccountId = account.Accountid,
                Username = account.Username,
                Email = account.Email,
                Fullname = account.Fullname,
                Phone = account.Phone,
                Role = account.Role,
                Status = account.Status
            };
        }

        public async Task UpdateStatusAsync(int id, UpdateAccountStatusRequest req)
        {
            var repo = _unitOfWork.GetRepository<Account>();
            var account = await repo.GetByIdAsync(id);

            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            // Kiểm tra trạng thái gửi lên có hợp lệ không
            var validStatuses = new[] { AccountStatus.PENDING, AccountStatus.ACTIVE, AccountStatus.INACTIVE, AccountStatus.DELETED };
            if (!validStatuses.Contains(req.Status))
            {
                throw new Exception($"Trạng thái '{req.Status}' không hợp lệ.");
            }

            // CHỈ cập nhật Status
            account.Status = req.Status;

            await repo.UpdateAsync(account);
        }

        public async Task DeleteAccountAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Account>();
            var account = await repo.GetByIdAsync(id);

            if (account == null) throw new Exception("Tài khoản không tồn tại.");

            // Soft Delete bằng cách đổi trạng thái
            account.Status = AccountStatus.DELETED;
            await repo.UpdateAsync(account);
        }
    }
}