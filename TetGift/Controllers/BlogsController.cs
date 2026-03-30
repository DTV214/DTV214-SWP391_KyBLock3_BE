using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        // Các API lấy danh sách/chi tiết thường để public cho khách vãng lai xem
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
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Create([FromBody] CreateBlogRequest req)
        {
            // Bóc tách AccountId từ Token của người đang đăng nhập
            var accountIdClaim = User.FindFirst("AccountId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
            {
                return Unauthorized("Không tìm thấy thông tin người dùng trong Token.");
            }

            if (!int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return BadRequest("Invalid Account Id in token.");
            }

            // Gọi service thực hiện lưu xuống DB với AccountId thật
            var result = await _blogService.CreateAsync(accountId, req);
            return Ok(result);
        }

        // PUT: api/blogs/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBlogRequest req)
        {
            await _blogService.UpdateAsync(id, req);
            return Ok(new { message = "Cập nhật bài viết thành công." });
        }

        // DELETE: api/blogs/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> Delete(int id)
        {
            await _blogService.DeleteAsync(id);
            return Ok(new { message = "Xóa bài viết thành công." });
        }
    }
}