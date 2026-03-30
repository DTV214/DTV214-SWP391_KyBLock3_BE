using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TetGift.BLL.Dtos
{
    public class OrderQueryParameters
    {
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public string? Status { get; set; }

        [BindNever]
        public int? AccountId { get; set; } = 0;
    }
}
