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
                            CreationDate = b.Creationdate
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
                CreationDate = blog.Creationdate
            };
        }

        public async Task<BlogDto> CreateAsync(int accountId, CreateBlogRequest req)
        {
            var blogRepo = _unitOfWork.GetRepository<Blog>();

            var newBlog = new Blog
            {
                Accountid = accountId, // Gán người tạo từ Token
                Title = req.Title,
                Content = req.Content,
                Creationdate = DateTime.Now,
                Isdeleted = false
            };

            await blogRepo.AddAsync(newBlog);
            await _unitOfWork.SaveAsync();

            // Để trả về tên tác giả ngay lập tức, ta cần query lại Account hoặc lấy từ context (ở đây trả về ID trước cho nhanh)
            // Hoặc đơn giản return DTO với thông tin vừa tạo
            return new BlogDto
            {
                BlogId = newBlog.Blogid,
                Title = newBlog.Title,
                Content = newBlog.Content,
                AuthorName = "You (Just created)", // Frontend sẽ reload lại list để thấy tên
                CreationDate = newBlog.Creationdate
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

            // Không cập nhật CreationDate, giữ nguyên người tạo

            await blogRepo.UpdateAsync(blog);
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
        }
    }
}
