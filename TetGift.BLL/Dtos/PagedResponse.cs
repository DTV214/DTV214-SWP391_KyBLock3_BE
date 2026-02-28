namespace TetGift.BLL.Dtos
{
    public class PagedResponse<T>
    {
        public PagedResponse() { }

        public PagedResponse(IEnumerable<T> data, int totalItems, int pageNumber, int pageSize)
        {
            Data = data;
            TotalItems = totalItems;
            CurrentPage = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
        }

        public IEnumerable<T> Data { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
    }
}
