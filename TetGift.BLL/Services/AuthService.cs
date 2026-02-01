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

        public AuthService(
            IUnitOfWork uow,
            IConfiguration cfg,
            IEmailSender email,
            IEmailTemplateRenderer tpl,
            IJwtTokenGenerator jwt)
        {
            _uow = uow;
            _cfg = cfg;
            _email = email;
            _tpl = tpl;
            _jwt = jwt;
        }

        public async Task RequestRegisterOtpAsync(RegisterRequest req)
        {
            var username = (req.Username ?? "").Trim();
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var password = req.Password ?? "";

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
                throw new Exception("Username/Email/Password is required.");

            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            var otpMinutes = int.TryParse(_cfg["Otp:ExpireMinutes"], out var m) ? m : 5;

            var repo = _uow.GetRepository<Account>();

            // Không dùng FirstOrDefaultAsync để BLL không cần EF Core package
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
                    Role = "Customer",
                    Status = AccountStatus.PENDING
                };

                await repo.AddAsync(acc);
            }
            else
            {
                // Resend OTP cho account Pending
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
            if (acc.Status == AccountStatus.ACTIVE)
                throw new Exception("Account already verified.");

            if (acc.RegisterOtpExpiresAt == null || acc.RegisterOtpExpiresAt < DateTime.Now)
                throw new Exception("OTP expired.");

            if (acc.RegisterOtpFailCount >= maxFail)
                throw new Exception("Too many failed OTP attempts.");

            if (string.IsNullOrWhiteSpace(acc.RegisterOtpHash))
                throw new Exception("OTP not requested.");

            var inputHash = OtpHelper.HashOtp(otp, otpSecret);
            var ok = OtpHelper.FixedEqualsBase64(acc.RegisterOtpHash, inputHash);

            if (!ok)
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

            var token = _jwt.Generate(acc);

            return new AuthResultDto
            {
                Token = token,
                AccountId = acc.Accountid,
                Username = acc.Username,
                Email = acc.Email,
                Role = acc.Role
            };
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequest req)
        {
            var username = (req.Username ?? "").Trim();
            var password = req.Password ?? "";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new Exception("Username/Password is required.");

            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Username == username)).FirstOrDefault();

            if (acc == null) throw new Exception("Invalid credentials.");
            if (acc.Status != AccountStatus.ACTIVE)
                throw new Exception("Account not verified.");

            if (!PasswordHasher.Verify(password, acc.Password))
                throw new Exception("Invalid credentials.");

            var token = _jwt.Generate(acc);

            return new AuthResultDto
            {
                Token = token,
                AccountId = acc.Accountid,
                Username = acc.Username,
                Email = acc.Email,
                Role = acc.Role
            };
        }
    }
}
