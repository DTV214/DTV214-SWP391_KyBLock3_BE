using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;
// Đã bỏ using Microsoft.AspNetCore.Http và System.IO vì không xử lý file vật lý nữa

namespace TetGift.BLL.Services
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BlogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ĐÃ XÓA hàm SaveFileAsync ở đây vì không cần lưu file local nữa

        public async Task<IEnumerable<BlogDto>> GetAllAsync()
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

            var blogs = await blogRepo.GetAllAsync(
                predicate: b => b.Isdeleted != true,
                include: q => q.Include(b => b.Account)
            );

            return blogs.OrderByDescending(b => b.Creationdate)
                        .Select(b => new BlogDto
                        {
                            BlogId = b.Blogid,
                            Title = b.Title ?? "",
                            Content = b.Content ?? "",
                            AuthorName = b.Account?.Fullname ?? b.Account?.Username ?? "Unknown",
                            CreationDate = b.Creationdate,
                            ImageUrl = b.ImageUrl,
                            VideoUrl = b.VideoUrl
                        });
        }

        public async Task<BlogDto> GetByIdAsync(int id)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

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
                ImageUrl = blog.ImageUrl,
                VideoUrl = blog.VideoUrl
            };
        }

        public async Task<BlogDto> CreateAsync(int accountId, CreateBlogRequest req)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

            var newBlog = new Blog
            {
                Accountid = accountId,
                Title = req.Title,
                Content = req.Content,
                Creationdate = DateTime.Now,
                Isdeleted = false,
                // Gán trực tiếp URL string nhận được từ Frontend vào Database
                ImageUrl = req.ImageUrl,
                VideoUrl = req.VideoUrl
            };

            await blogRepo.AddAsync(newBlog);
            await _unitOfWork.SaveAsync();

            return new BlogDto
            {
                BlogId = newBlog.Blogid,
                Title = newBlog.Title,
                Content = newBlog.Content,
                AuthorName = "You (Just created)",
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

            // Nếu FE gửi URL ảnh mới lên thì cập nhật
            if (!string.IsNullOrWhiteSpace(req.ImageUrl))
            {
                blog.ImageUrl = req.ImageUrl;
            }

            // Nếu FE gửi URL video mới lên thì cập nhật
            if (!string.IsNullOrWhiteSpace(req.VideoUrl))
            {
                blog.VideoUrl = req.VideoUrl;
            }

            await blogRepo.UpdateAsync(blog);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();
            var blog = await blogRepo.GetByIdAsync(id);

            if (blog == null || blog.Isdeleted == true)
                throw new Exception("Bài viết không tồn tại.");

            blog.Isdeleted = true;
            await blogRepo.UpdateAsync(blog);
            await _unitOfWork.SaveAsync();
        }
    }
}