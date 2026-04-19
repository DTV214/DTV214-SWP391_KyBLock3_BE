using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, List<EmailAttachmentDto>? attachments = null
        );
    }
}
