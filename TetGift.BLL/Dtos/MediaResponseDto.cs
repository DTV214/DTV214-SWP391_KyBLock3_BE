using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetGift.BLL.Dtos
{
    public class MediaResponseDto
    {
        public string Url { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public string ResourceType { get; set; } = null!; // "image" hoặc "video"
    }
}
