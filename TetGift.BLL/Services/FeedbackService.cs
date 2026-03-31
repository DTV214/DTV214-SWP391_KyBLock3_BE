using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Interfaces;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _uow;

    public FeedbackService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<FeedbackResponseDto> AddFeedbackAsync(int orderId, int accountId, CreateFeedbackRequest request)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var feedbackRepo = _uow.GetRepository<Feedback>();

        var orders = await orderRepo.FindAsync(o => o.Orderid == orderId && o.Accountid == accountId);
        if (!orders.Any())
            throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền viết đánh giá cho đơn hàng này.");

        var order = orders.First();
        if (order.Status != OrderStatus.DELIVERED)
            throw new Exception("Bạn chỉ có thể đánh giá sau khi đã nhận hàng (Đã giao).");

        var existingFeedbacks = await feedbackRepo.FindAsync(f => f.Orderid == orderId && f.Isdeleted != true);
        if (existingFeedbacks.Any())
            throw new Exception("Đơn hàng này đã được đánh giá.");

        var feedback = new Feedback
        {
            Orderid = orderId,
            Accountid = accountId,
            Rating = request.Rating,
            Comment = request.Comment,
            Isdeleted = false
        };

        await feedbackRepo.AddAsync(feedback);
        await _uow.SaveAsync();

        return new FeedbackResponseDto
        {
            FeedbackId = feedback.Feedbackid,
            OrderId = feedback.Orderid ?? 0,
            Rating = feedback.Rating ?? 0,
            Comment = feedback.Comment
        };
    }

    public async Task<FeedbackResponseDto> UpdateFeedbackAsync(int feedbackId, int accountId, UpdateFeedbackRequest request)
    {
        var feedbackRepo = _uow.GetRepository<Feedback>();
        var feedbacks = await feedbackRepo.FindAsync(f => f.Feedbackid == feedbackId && f.Accountid == accountId && f.Isdeleted != true);
        
        if (!feedbacks.Any())
            throw new Exception("Không tìm thấy bình luận hoặc bạn không có quyền sửa.");

        var feedback = feedbacks.First();
        feedback.Rating = request.Rating;
        feedback.Comment = request.Comment;

        feedbackRepo.Update(feedback);
        await _uow.SaveAsync();

        return new FeedbackResponseDto
        {
            FeedbackId = feedback.Feedbackid,
            OrderId = feedback.Orderid ?? 0,
            Rating = feedback.Rating ?? 0,
            Comment = feedback.Comment
        };
    }

    public async Task DeleteFeedbackAsync(int feedbackId, int accountId)
    {
        var feedbackRepo = _uow.GetRepository<Feedback>();
        var feedbacks = await feedbackRepo.FindAsync(f => f.Feedbackid == feedbackId && f.Accountid == accountId);
        
        if (!feedbacks.Any())
            throw new Exception("Không tìm thấy bình luận hoặc bạn không có quyền xóa.");

        var feedback = feedbacks.First();
        
        // Cần xóa cứng theo yêu cầu user
        feedbackRepo.Delete(feedback);
        await _uow.SaveAsync();
    }

    public async Task<List<FeedbackResponseDto>> GetFeedbacksForProductAsync(int productId)
    {
        var feedbackRepo = _uow.GetRepository<Feedback>();

        var feedbacks = await feedbackRepo.Entities
            .Include(f => f.Account)
            .Include(f => f.Order)
            .Where(f => f.Isdeleted != true && f.Order != null && f.Order.OrderDetails.Any(od => od.Productid == productId))
            .OrderByDescending(f => f.Feedbackid)
            .ToListAsync();

        return feedbacks.Select(f => {
            var rawName = f.Account?.Fullname ?? "Khách hàng";
            var parts = rawName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var anonymousName = "K*** H***";
            if (parts.Length == 1) 
            {
                anonymousName = parts[0].Length > 1 
                                ? parts[0].Substring(0, 1) + "***" 
                                : parts[0] + "***";
            }
            else if (parts.Length >= 2)
            {
                var first = parts[0].Length > 1 ? parts[0].Substring(0, 1) + "***" : parts[0] + "***";
                var last = parts[parts.Length - 1].Length > 1 ? parts[parts.Length - 1].Substring(0, 1) + "***" : parts[parts.Length - 1] + "***";
                anonymousName = first + " " + last;
            }

            return new FeedbackResponseDto
            {
                FeedbackId = f.Feedbackid,
                OrderId = f.Orderid ?? 0,
                Rating = f.Rating ?? 0,
                Comment = f.Comment,
                CustomerName = anonymousName
            };
        }).ToList();
    }
}
