using Microsoft.Extensions.Configuration;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Common.Securities;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _cfg;
        private readonly IEmailSender _email;
        private readonly IEmailTemplateRenderer _tpl;
        private readonly IJwtTokenGenerator _jwt;
        private readonly ICacheService _cache;

        public AuthService(
            IUnitOfWork uow,
            IConfiguration cfg,
            IEmailSender email,
            IEmailTemplateRenderer tpl,
            IJwtTokenGenerator jwt,
            ICacheService cache)
        {
            _uow = uow;
            _cfg = cfg;
            _email = email;
            _tpl = tpl;
            _jwt = jwt;
            _cache = cache;
        }

        // --- CÁC HÀM LOGIN / REGISTER GIỮ NGUYÊN NHƯ CŨ ---
        public async Task RequestRegisterOtpAsync(RegisterRequest req)
        {
            var username = (req.Username ?? "").Trim();
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var password = req.Password ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new Exception("Username/Email/Password is required.");

            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            var otpMinutes = int.TryParse(_cfg["Otp:ExpireMinutes"], out var m) ? m : 5;
            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Username == username)).FirstOrDefault();

            if (acc != null && acc.Status == AccountStatus.ACTIVE)
                throw new Exception("Account already active.");

            if (acc == null)
            {
                acc = new Account
                {
                    Username = username,
                    Password = PasswordHasher.Hash(password),
                    Email = email,
                    Fullname = req.Fullname,
                    Phone = req.Phone,
                    Role = UserRole.CUSTOMER,
                    Status = AccountStatus.PENDING
                };
                await repo.AddAsync(acc);
            }
            else
            {
                acc.Email = email;
                acc.Fullname = req.Fullname ?? acc.Fullname;
                acc.Phone = req.Phone ?? acc.Phone;
                acc.Password = PasswordHasher.Hash(password);
                acc.Status = AccountStatus.PENDING;
                repo.Update(acc);
            }

            var otp = OtpHelper.Generate6();
            acc.RegisterOtpHash = OtpHelper.HashOtp(otp, otpSecret);
            acc.RegisterOtpExpiresAt = DateTime.Now.AddMinutes(otpMinutes);
            acc.RegisterOtpFailCount = 0;
            acc.RegisterOtpVerifiedAt = null;

            await _uow.SaveAsync();
            var html = _tpl.RenderOtp(otp, otpMinutes);
            await _email.SendAsync(email, "TetGift - OTP xác thực đăng ký", html);
        }

        public async Task<AuthResultDto> VerifyRegisterOtpAsync(VerifyOtpRequest req)
        {
            var username = (req.Username ?? "").Trim();
            var otp = (req.Otp ?? "").Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(otp))
                throw new Exception("Username/OTP is required.");

            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            var maxFail = int.TryParse(_cfg["Otp:MaxFail"], out var mf) ? mf : 5;
            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Username == username)).FirstOrDefault();

            if (acc == null) throw new Exception("Account not found.");
            if (acc.Status == AccountStatus.ACTIVE) throw new Exception("Account already verified.");
            if (acc.RegisterOtpExpiresAt == null || acc.RegisterOtpExpiresAt < DateTime.Now) throw new Exception("OTP expired.");
            if (acc.RegisterOtpFailCount >= maxFail) throw new Exception("Too many failed OTP attempts.");

            var inputHash = OtpHelper.HashOtp(otp, otpSecret);
            if (!OtpHelper.FixedEqualsBase64(acc.RegisterOtpHash ?? "", inputHash))
            {
                acc.RegisterOtpFailCount++;
                repo.Update(acc);
                await _uow.SaveAsync();
                throw new Exception("OTP invalid.");
            }

            acc.Status = AccountStatus.ACTIVE;
            acc.RegisterOtpVerifiedAt = DateTime.Now;
            acc.RegisterOtpHash = null;
            acc.RegisterOtpExpiresAt = null;
            acc.RegisterOtpFailCount = 0;

            repo.Update(acc);
            await _uow.SaveAsync();
            return new AuthResultDto { Token = _jwt.Generate(acc), AccountId = acc.Accountid, Username = acc.Username, Email = acc.Email, Role = (acc.Role ?? UserRole.CUSTOMER).ToUpper() };
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequest req)
        {
            var username = (req.Username ?? "").Trim();
            var password = req.Password ?? "";
            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Username == username)).FirstOrDefault();

            if (acc == null || acc.Status != AccountStatus.ACTIVE || !PasswordHasher.Verify(password, acc.Password))
                throw new Exception("Invalid credentials or account not verified.");

            return new AuthResultDto { Token = _jwt.Generate(acc), AccountId = acc.Accountid, Username = acc.Username, Email = acc.Email, Role = (acc.Role ?? UserRole.CUSTOMER).ToUpper() };
        }

        // --- ĐÃ TỐI ƯU: FORGET PASSWORD (XỬ LÝ 1 EMAIL NHIỀU ACC) ---

        public async Task RequestForgotPasswordOtpAsync(ForgotPasswordRequest req)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var username = (req.Username ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username))
                throw new Exception("Vui lòng nhập đầy đủ Email và Tên đăng nhập.");

            var repo = _uow.GetRepository<Account>();

            // Tìm chính xác tài khoản khớp cả Email VÀ Username
            var acc = (await repo.FindAsync(a =>
                a.Email == email &&
                a.Username == username &&
                a.Status == AccountStatus.ACTIVE)).FirstOrDefault();

            if (acc == null)
                throw new Exception("Thông tin tài khoản không chính xác hoặc tài khoản chưa kích hoạt.");

            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            var otpMinutes = int.TryParse(_cfg["Otp:ExpireMinutes"], out var m) ? m : 5;

            var otp = OtpHelper.Generate6();
            var otpHash = OtpHelper.HashOtp(otp, otpSecret);

            // Key Redis chứa cả Email và Username để tránh xung đột
            var cacheKey = $"pwd_reset_otp_{email}_{username}";
            await _cache.SetAsync(cacheKey, otpHash, TimeSpan.FromMinutes(otpMinutes));

            var html = _tpl.RenderOtp(otp, otpMinutes);
            await _email.SendAsync(email, "TetGift - Mã xác thực khôi phục mật khẩu", html);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest req)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var username = (req.Username ?? "").Trim();
            var otp = (req.Otp ?? "").Trim();
            var newPwd = req.NewPassword ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPwd))
                throw new Exception("Vui lòng điền đầy đủ các thông tin yêu cầu.");

            // 1. Kiểm tra OTP từ Redis theo tổ hợp Email + Username
            var cacheKey = $"pwd_reset_otp_{email}_{username}";
            var savedHash = await _cache.GetAsync<string>(cacheKey);

            if (string.IsNullOrWhiteSpace(savedHash))
                throw new Exception("Mã xác thực đã hết hạn hoặc không tồn tại.");

            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            if (!OtpHelper.FixedEqualsBase64(savedHash, OtpHelper.HashOtp(otp, otpSecret)))
                throw new Exception("Mã xác thực không chính xác.");

            // 2. Cập nhật mật khẩu
            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Email == email && a.Username == username)).FirstOrDefault();

            if (acc == null) throw new Exception("Tài khoản không tồn tại.");

            acc.Password = PasswordHasher.Hash(newPwd);
            repo.Update(acc);
            await _uow.SaveAsync();

            // 3. Xóa cache sau khi thành công
            await _cache.RemoveByPrefixAsync(cacheKey);
        }
    }
}