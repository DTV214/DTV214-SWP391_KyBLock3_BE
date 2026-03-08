using Microsoft.AspNetCore.Http; // Bắt buộc để nhận diện IFormFile
using System.IO; // Bắt buộc để xử lý thư mục và lưu file vật lý
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // --- HÀM HỖ TRỢ LƯU FILE ---
        private async Task<string?> SaveFileAsync(IFormFile? file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            // Đường dẫn lưu file: wwwroot/uploads/[subFolder]
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Tạo tên file ngẫu nhiên để không bị trùng lặp
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            // Tiến hành copy file vào server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về đường dẫn để lưu vào Database
            return $"/uploads/{subFolder}/{fileName}";
        }
        // ---------------------------

        public async Task<IEnumerable<BlogDto>> GetAllAsync()
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

            // Lấy tất cả bài chưa xóa, kèm thông tin người viết (Account)
            var blogs = await blogRepo.GetAllAsync(
                predicate: b => b.Isdeleted != true,
                include: q => q.Include(b => b.Account)
            );

            // Sắp xếp bài mới nhất lên đầu và map sang DTO
            return blogs.OrderByDescending(b => b.Creationdate)
                        .Select(b => new BlogDto
                        {
                            BlogId = b.Blogid,
                            Title = b.Title ?? "",
                            Content = b.Content ?? "",
                            AuthorName = b.Account?.Fullname ?? b.Account?.Username ?? "Unknown",
                            CreationDate = b.Creationdate,
                            ImageUrl = b.ImageUrl, // Lấy đường dẫn ảnh
                            VideoUrl = b.VideoUrl  // Lấy đường dẫn video
                        });
        }

        public async Task<BlogDto> GetByIdAsync(int id)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

            // Tìm bài viết theo ID và include Account
            var blog = await blogRepo.FindAsync(
                predicate: b => b.Blogid == id && b.Isdeleted != true,
                include: q => q.Include(b => b.Account)
            );

            if (blog == null) throw new Exception("Bài viết không tồn tại.");

            return new BlogDto
            {
                BlogId = blog.Blogid,
                Title = blog.Title ?? "",
                Content = blog.Content ?? "",
                AuthorName = blog.Account?.Fullname ?? blog.Account?.Username ?? "Unknown",
                CreationDate = blog.Creationdate,
                ImageUrl = blog.ImageUrl, // Lấy đường dẫn ảnh
                VideoUrl = blog.VideoUrl  // Lấy đường dẫn video
            };
        }

        public async Task<BlogDto> CreateAsync(int accountId, CreateBlogRequest req)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

            var newBlog = new Blog
            {
                Accountid = accountId, // Gán người tạo từ Token/Mock
                Title = req.Title,
                Content = req.Content,
                Creationdate = DateTime.Now,
                Isdeleted = false,
                ImageUrl = await SaveFileAsync(req.ImageFile, "blogs/images"), // Lưu file ảnh và lấy URL
                VideoUrl = await SaveFileAsync(req.VideoFile, "blogs/videos")  // Lưu file video và lấy URL
            };

            await blogRepo.AddAsync(newBlog);
            await _unitOfWork.SaveAsync();

            return new BlogDto
            {
                BlogId = newBlog.Blogid,
                Title = newBlog.Title,
                Content = newBlog.Content,
                AuthorName = "You (Just created)", // Frontend sẽ reload lại list để thấy tên
                CreationDate = newBlog.Creationdate,
                ImageUrl = newBlog.ImageUrl,
                VideoUrl = newBlog.VideoUrl
            };
        }

        public async Task UpdateAsync(int id, UpdateBlogRequest req)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();
            var blog = await blogRepo.GetByIdAsync(id);

            if (blog == null || blog.Isdeleted == true)
                throw new Exception("Bài viết không tồn tại.");

            blog.Title = req.Title;
            blog.Content = req.Content;

            // Chỉ cập nhật ảnh nếu có file mới được đẩy lên
            if (req.ImageFile != null)
            {
                blog.ImageUrl = await SaveFileAsync(req.ImageFile, "blogs/images");
            }

            // Chỉ cập nhật video nếu có file mới được đẩy lên
            if (req.VideoFile != null)
            {
                blog.VideoUrl = await SaveFileAsync(req.VideoFile, "blogs/videos");
            }

            // Không cập nhật CreationDate, giữ nguyên người tạo

            await blogRepo.UpdateAsync(blog);
            await _unitOfWork.SaveAsync(); // Bắt buộc gọi để lưu xuống DB
        }

        public async Task DeleteAsync(int id)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();
            var blog = await blogRepo.GetByIdAsync(id);

            if (blog == null || blog.Isdeleted == true)
                throw new Exception("Bài viết không tồn tại.");

            // Soft Delete
            blog.Isdeleted = true;
            await blogRepo.UpdateAsync(blog);
            await _unitOfWork.SaveAsync(); // Bắt buộc gọi để lưu xuống DB
        }
    }
}