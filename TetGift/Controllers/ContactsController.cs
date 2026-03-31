using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/contacts")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactsController(IContactService contactService)
        {
            _contactService = contactService;
        }

        // 1. PUBLIC: Khách hàng gửi form
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendRequest([FromBody] CreateContactRequest req)
        {
            await _contactService.CreateRequestAsync(req);
            return Ok(new { message = "Gửi yêu cầu thành công. Chúng tôi sẽ sớm liên hệ với bạn." });
        }

        // 2. ADMIN: Xem danh sách
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _contactService.GetAllRequestsAsync();
            return Ok(result);
        }

        // 3. ADMIN: Sửa thông tin
        [HttpPut("admin/{id}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateContactRequest req)
        {
            await _contactService.UpdateRequestAsync(id, req);
            return Ok(new { message = "Cập nhật yêu cầu thành công." });
        }

        // 4. ADMIN: Xóa (Dọn rác)
        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Delete(int id)
        {
            await _contactService.DeleteRequestAsync(id);
            return Ok(new { message = "Đã xóa yêu cầu." });
        }
    }
}