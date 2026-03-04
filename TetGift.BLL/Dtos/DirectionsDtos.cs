namespace TetGift.BLL.Dtos
{
    public class DirectionsRequest
    {
        public double FromLat { get; set; }
        public double FromLng { get; set; }
        public string? TravelMode { get; set; } = "driving";
    }

    public class DirectionsResponse
    {
        public int StoreLocationId { get; set; }
        public string Url { get; set; } = default!;
    }
}
