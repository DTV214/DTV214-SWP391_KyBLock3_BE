using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    // Chỉ định quyền Admin mới được phép gọi các API này
    public class AdminAccountsController : ControllerBase
    {
        private readonly IAdminAccountService _adminAccountService;

        public AdminAccountsController(IAdminAccountService adminAccountService)
        {
            _adminAccountService = adminAccountService;
        }

        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _adminAccountService.GetAllAccountsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN,STAFF")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _adminAccountService.GetAccountByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountAdminRequest req)
        {
            var result = await _adminAccountService.CreateAccountAsync(req);
            return Ok(result);
        }

        // Khóa chặt việc Update bằng hàm PATCH hoặc PUT nhưng body chỉ có Status
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAccountStatusRequest req)
        {
            await _adminAccountService.UpdateStatusAsync(id, req);
            return Ok(new { message = "Cập nhật trạng thái thành công." });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _adminAccountService.DeleteAccountAsync(id);
            return Ok(new { message = "Khóa (xóa) tài khoản thành công." });
        }
    }
}