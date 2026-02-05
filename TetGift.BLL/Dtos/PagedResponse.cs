namespace TetGift.BLL.Dtos
{
    public class PagedResponse<T>(IEnumerable<T> data, int totalItems, int pageNumber, int pageSize)
    {
        public IEnumerable<T> Data { get; set; } = data;
        public int CurrentPage { get; set; } = pageNumber;
        public int TotalPages { get; set; } = (int)Math.Ceiling(totalItems / (double)pageSize);
        public int TotalItems { get; set; } = totalItems;
        public int PageSize { get; set; } = pageSize;
    }
}
