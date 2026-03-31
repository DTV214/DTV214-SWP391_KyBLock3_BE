using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;
// Đảm bảo bạn đang using đúng namespace chứa IEmailSender của dự án (thường nằm ở Common hoặc Interfaces)

namespace TetGift.BLL.Services
{
    public class ContactService : IContactService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailSender _emailSender; // Inject service gửi mail của bạn vào đây

        public ContactService(IUnitOfWork uow, IEmailSender emailSender)
        {
            _uow = uow;
            _emailSender = emailSender;
        }

        public async Task CreateRequestAsync(CreateContactRequest req)
        {
            var contact = new RequestContact
            {
                CustomerName = req.CustomerName,
                Phone = req.Phone,
                Email = req.Email,
                Note = req.Note,
                CreatedAt = DateTime.Now,
                // BƯỚC 1: Khách mới gửi -> Mặc định là FALSE (Pending), KHÔNG GỬI MAIL.
                IsContacted = false
            };

            await _uow.GetRepository<RequestContact>().AddAsync(contact);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<ContactAdminDto>> GetAllRequestsAsync()
        {
            var items = await _uow.GetRepository<RequestContact>().GetAllAsync();
            return items.OrderByDescending(x => x.CreatedAt).Select(x => new ContactAdminDto
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                Email = x.Email,
                Phone = x.Phone,
                Note = x.Note,
                IsContacted = x.IsContacted,
                CreatedAt = x.CreatedAt
            });
        }

        public async Task UpdateRequestAsync(int id, UpdateContactRequest req)
        {
            var repo = _uow.GetRepository<RequestContact>();
            var item = await repo.GetByIdAsync(id);
            if (item == null) throw new Exception("Yêu cầu không tồn tại.");

            // BƯỚC 2: LOGIC GỬI MAIL THÔNG MINH
            // Kiểm tra xem Staff có đang chuyển trạng thái từ False -> True hay không?
            bool isNewlyContacted = (item.IsContacted == false && req.IsContacted == true);

            // Cập nhật thông tin Admin/Staff nhập vào
            item.CustomerName = req.CustomerName ?? item.CustomerName;
            item.Phone = req.Phone ?? item.Phone;
            item.Email = req.Email ?? item.Email;
            item.Note = req.Note ?? item.Note;
            item.IsContacted = req.IsContacted;

            await repo.UpdateAsync(item);
            // Có thể cần gọi thêm await _uow.SaveAsync() ở đây tùy thuộc vào cách bạn thiết kế Repository
            await _uow.SaveAsync();

            // BƯỚC 3: NẾU STAFF XÁC NHẬN -> GỬI MAIL CHO KHÁCH
            if (isNewlyContacted && !string.IsNullOrEmpty(item.Email))
            {
                try
                {
                    string subject = "Xác nhận yêu cầu liên hệ từ TetGift";
                    string htmlBody = $@"
                        <h3>Chào {item.CustomerName},</h3>
                        <p>Chúng tôi đã ghi nhận yêu cầu liên hệ của bạn.</p>
                        <p>Đội ngũ nhân viên sẽ sớm liên hệ và hỗ trợ bạn qua số điện thoại: <strong>{item.Phone}</strong>.</p>
                        <br/>
                        <p>Trân trọng,<br/><strong>TetGift Team</strong></p>";

                    await _emailSender.SendAsync(item.Email, subject, htmlBody);
                }
                catch
                {
                    // Catch để nếu lỗi cấu hình Email (sai password app...) thì vẫn lưu DB thành công, không bị văng lỗi 500
                }
            }
        }

        public async Task DeleteRequestAsync(int id)
        {
            var repo = _uow.GetRepository<RequestContact>();
            var item = await repo.GetByIdAsync(id);
            if (item == null) throw new Exception("Yêu cầu không tồn tại.");

            repo.Delete(item);
            await _uow.SaveAsync();
        }
    }
}