using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Common;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            try
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".mp4", ".avi", ".mov" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new ApiResponse<MediaResponseDto>
                    {
                        Status = 400,
                        Msg = "Chỉ chấp nhận ảnh (.jpg, .png, .jpeg) hoặc video (.mp4, .avi, .mov)",
                        Data = null
                    });
                }

                // Có thể thêm logic chặn dung lượng ở đây (vd: file.Length > 20MB thì chặn)

                var result = await _mediaService.UploadMediaAsync(file);

                return Ok(new ApiResponse<MediaResponseDto>
                {
                    Status = 200,
                    Msg = "Upload thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<MediaResponseDto>
                {
                    Status = 400,
                    Msg = ex.Message,
                    Data = null
                });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteMedia([FromQuery] string publicId, [FromQuery] string resourceType = "image")
        {
            try
            {
                if (string.IsNullOrEmpty(publicId))
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Status = 400,
                        Msg = "Thiếu tham số publicId",
                        Data = false
                    });
                }

                var isDeleted = await _mediaService.DeleteMediaAsync(publicId, resourceType);

                if (isDeleted)
                {
                    return Ok(new ApiResponse<bool>
                    {
                        Status = 200,
                        Msg = "Xóa file thành công",
                        Data = true
                    });
                }

                return BadRequest(new ApiResponse<bool>
                {
                    Status = 400,
                    Msg = "Không thể xóa file (có thể publicId không tồn tại)",
                    Data = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Status = 400,
                    Msg = ex.Message,
                    Data = false
                });
            }
        }
    }
}
