namespace TetGift.BLL.Dtos
{
    /// <summary>
    /// Simplified Product DTO for chatbot responses
    /// </summary>
    public class ProductChatDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int? Stock { get; set; }
    }
}