namespace TetGift.BLL.Dtos
{
    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = null!;
        public byte[] ContentBytes { get; set; } = null!;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
