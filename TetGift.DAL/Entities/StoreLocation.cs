namespace TetGift.DAL.Entities
{
    public partial class StoreLocation
    {
        public int StoreLocationId { get; set; }

        public string? Name { get; set; }
        public string? AddressLine { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public string? PhoneNumber { get; set; }
        public string? OpenHoursText { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
