using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize] // Bắt buộc phải đăng nhập mới vào được đây
    public class ProfileController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public ProfileController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // Helper function để lấy AccountId từ Token
        private int GetCurrentAccountId()
        {
            // Tìm claim "AccountId" trước (do bên Auth của bạn lưu tên này)
            var claim = User.FindFirst("AccountId")
                        ?? User.FindFirst(ClaimTypes.NameIdentifier); // Fallback

            if (claim == null || !int.TryParse(claim.Value, out int id))
            {
                throw new Exception("Token không hợp lệ hoặc không chứa AccountId.");
            }
            return id;
        }

        // GET: api/profile
        // Xem thông tin của chính mình
        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                int myId = GetCurrentAccountId();
                var result = await _accountService.GetProfileAsync(myId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // PUT: api/profile
        // Cập nhật thông tin của chính mình
        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest req)
        {
            try
            {
                int myId = GetCurrentAccountId();
                await _accountService.UpdateProfileAsync(myId, req);
                return Ok(new { message = "Cập nhật hồ sơ thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/profile
        // Tự khóa tài khoản của chính mình
        [HttpDelete]
        public async Task<IActionResult> DeactivateMyAccount()
        {
            try
            {
                int myId = GetCurrentAccountId();
                await _accountService.DeactivateAccountAsync(myId);
                return Ok(new { message = "Tài khoản đã được vô hiệu hóa thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}