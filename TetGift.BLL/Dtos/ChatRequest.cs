namespace TetGift.BLL.Dtos
{
    public class ChatRequest
    {
        public List<object>? History { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}