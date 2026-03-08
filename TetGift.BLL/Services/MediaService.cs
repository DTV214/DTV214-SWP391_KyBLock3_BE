using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Interfaces;
using TetGift.BLL.Settings;
using TetGift.BLL.Dtos;


namespace TetGift.BLL.Services
{
    public class MediaService : IMediaService
    {
        private readonly Cloudinary _cloudinary;

        public MediaService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<MediaResponseDto> UploadMediaAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) throw new Exception("File rỗng.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            var isVideo = extension == ".mp4" || extension == ".avi" || extension == ".mov";

            using var stream = file.OpenReadStream();

            // Dùng biến chứa kết quả chung của Cloudinary
            RawUploadResult uploadResult;

            if (isVideo)
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "TetGift_Media" // Đổi tên folder cho tổng quát
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "TetGift_Media"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            if (uploadResult.Error != null)
                throw new Exception($"Lỗi từ Cloudinary: {uploadResult.Error.Message}");

            return new MediaResponseDto
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId,
                ResourceType = isVideo ? "video" : "image"
            };
        }

        public async Task<bool> DeleteMediaAsync(string publicId, string resourceType)
        {
            // Phải chỉ định đúng ResourceType thì Cloudinary mới tìm và xóa được
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType.ToLower() == "video" ? ResourceType.Video : ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);

            // result.Result thường trả về "ok" nếu xóa thành công
            return result.Result == "ok";
        }
    }
}
