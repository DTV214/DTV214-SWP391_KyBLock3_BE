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

        // --- CÁC HÀM LOGIN / REGISTER GIỮ NGUYÊN ---
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
            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Username == username)).FirstOrDefault();

            if (acc == null) throw new Exception("Account not found.");

            var inputHash = OtpHelper.HashOtp(otp, otpSecret);
            if (!OtpHelper.FixedEqualsBase64(acc.RegisterOtpHash ?? "", inputHash))
                throw new Exception("OTP invalid.");

            acc.Status = AccountStatus.ACTIVE;
            acc.RegisterOtpVerifiedAt = DateTime.Now;
            acc.RegisterOtpHash = null;
            acc.RegisterOtpExpiresAt = null;

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

        // --- CẬP NHẬT THÔNG MINH: FORGET PASSWORD (XỬ LÝ ĐA TÀI KHOẢN) ---

        public async Task RequestForgotPasswordOtpAsync(ForgotPasswordRequest req)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var usernameInput = (req.Username ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Vui lòng nhập Email liên kết.");

            var repo = _uow.GetRepository<Account>();

            // 1. Lấy tất cả tài khoản ACTIVE liên kết với Email này
            var accounts = (await repo.FindAsync(a => a.Email == email && a.Status == AccountStatus.ACTIVE)).ToList();

            if (accounts.Count == 0)
                throw new Exception("Email này không tồn tại trên hệ thống hoặc chưa được kích hoạt.");

            Account? targetAcc = null;

            // 2. Logic xử lý đa tài khoản
            if (accounts.Count > 1 && string.IsNullOrWhiteSpace(usernameInput))
            {
                // TRƯỜNG HỢP: Quên Username khi có nhiều acc
                var usernameList = string.Join(", ", accounts.Select(a => a.Username));
                var reminderMsg = $"<div style='font-family: sans-serif;'> " +
                                  $"<p>Chào bạn, Email của bạn hiện đang liên kết với <b>{accounts.Count} tài khoản</b>:</p>" +
                                  $"<ul style='color: #d32f2f; font-weight: bold;'> {string.Join("", accounts.Select(a => $"<li>{a.Username}</li>"))} </ul>" +
                                  $"<p>Vui lòng quay lại trang Quên mật khẩu và nhập chính xác <b>Tên đăng nhập</b> bạn muốn khôi phục.</p></div>";

                await _email.SendAsync(email, "TetGift - Nhắc nhở danh sách Tên đăng nhập", reminderMsg);

                // Trả về lỗi đặc biệt để FE biết và yêu cầu user nhập Username
                throw new Exception("Email liên kết với nhiều tài khoản. Chúng tôi đã gửi danh sách Tên đăng nhập vào Email của bạn, hãy kiểm tra và thử lại.");
            }

            if (!string.IsNullOrWhiteSpace(usernameInput))
            {
                targetAcc = accounts.FirstOrDefault(a => a.Username.Equals(usernameInput, StringComparison.OrdinalIgnoreCase));
                if (targetAcc == null) throw new Exception("Tên đăng nhập không khớp với Email đã cung cấp.");
            }
            else
            {
                // Chỉ có 1 tài khoản duy nhất gắn với Email
                targetAcc = accounts.First();
            }

            // 3. Tạo và lưu OTP vào Redis
            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            var otpMinutes = int.TryParse(_cfg["Otp:ExpireMinutes"], out var m) ? m : 5;

            var otp = OtpHelper.Generate6();
            var otpHash = OtpHelper.HashOtp(otp, otpSecret);

            var cacheKey = $"pwd_reset_otp_{email}_{targetAcc.Username}";
            await _cache.SetAsync(cacheKey, otpHash, TimeSpan.FromMinutes(otpMinutes));

            // 4. Gửi mã OTP
            var html = _tpl.RenderOtp(otp, otpMinutes);
            await _email.SendAsync(email, $"TetGift - Mã khôi phục mật khẩu cho tài khoản: {targetAcc.Username}", html);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest req)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var username = (req.Username ?? "").Trim();
            var otp = (req.Otp ?? "").Trim();
            var newPwd = req.NewPassword ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPwd))
                throw new Exception("Vui lòng nhập đầy đủ các thông tin để đặt lại mật khẩu.");

            var cacheKey = $"pwd_reset_otp_{email}_{username}";
            var savedHash = await _cache.GetAsync<string>(cacheKey);

            if (string.IsNullOrWhiteSpace(savedHash))
                throw new Exception("Mã xác thực đã hết hạn hoặc bạn chưa yêu cầu cấp mã.");

            var otpSecret = _cfg["Otp:Secret"] ?? throw new Exception("Missing config: Otp:Secret");
            if (!OtpHelper.FixedEqualsBase64(savedHash, OtpHelper.HashOtp(otp, otpSecret)))
                throw new Exception("Mã xác thực không đúng.");

            var repo = _uow.GetRepository<Account>();
            var acc = (await repo.FindAsync(a => a.Email == email && a.Username == username)).FirstOrDefault();

            if (acc == null) throw new Exception("Hệ thống không tìm thấy tài khoản tương ứng.");

            acc.Password = PasswordHasher.Hash(newPwd);
            repo.Update(acc);
            await _uow.SaveAsync();

            await _cache.RemoveByPrefixAsync(cacheKey);
        }
    }
}