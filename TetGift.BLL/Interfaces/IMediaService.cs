using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IMediaService
    {
        // Đổi tên cho tổng quát (Media thay vì Image)
        Task<MediaResponseDto> UploadMediaAsync(IFormFile file);

        // Hàm mới để xóa file
        Task<bool> DeleteMediaAsync(string publicId, string resourceType);
    }
}
