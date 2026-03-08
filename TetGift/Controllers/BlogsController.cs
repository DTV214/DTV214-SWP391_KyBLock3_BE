using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/blogs")]
    public class BlogsController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogsController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        // GET: api/blogs
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _blogService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/blogs/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _blogService.GetByIdAsync(id);
            return Ok(result);
        }

        // POST: api/blogs
        [HttpPost]
        // [Authorize(Roles = "Admin,Staff")] // <--- ĐÃ COMMENT ĐỂ TEST
        public async Task<IActionResult> Create([FromForm] CreateBlogRequest req) // <--- ĐÃ SỬA: [FromBody] -> [FromForm]
        {
            // --- ĐÃ COMMENT LOGIC LẤY TỪ TOKEN ---
            /*
            var accountIdClaim = User.FindFirst("AccountId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            if (!int.TryParse(accountIdClaim.Value, out int accountId))
            {
                 return BadRequest("Invalid Account Id in token.");
            }
            */

            // --- TEST: GIẢ LẬP NGƯỜI DÙNG CÓ ID = 1 ---
            // (Đảm bảo trong bảng Account có dòng id = 1)
            int accountId = 1;
            // -------------------------------------------------------------

            var result = await _blogService.CreateAsync(accountId, req);
            return Ok(result);
        }

        // PUT: api/blogs/{id}
        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin,Staff")] // <--- ĐÃ COMMENT ĐỂ TEST
        public async Task<IActionResult> Update(int id, [FromForm] UpdateBlogRequest req) // <--- ĐÃ SỬA: [FromBody] -> [FromForm]
        {
            await _blogService.UpdateAsync(id, req);
            return Ok(new { message = "Cập nhật bài viết thành công." });
        }

        // DELETE: api/blogs/{id}
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin,Staff")] // <--- ĐÃ COMMENT ĐỂ TEST
        public async Task<IActionResult> Delete(int id)
        {
            await _blogService.DeleteAsync(id);
            return Ok(new { message = "Xóa bài viết thành công." });
        }
    }
}