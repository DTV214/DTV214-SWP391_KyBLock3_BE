namespace TetGift.DAL.Entities
{
    public partial class AccountAddress
    {
        public int AccountAddressId { get; set; }

        public int AccountId { get; set; }

        public string? Label { get; set; }
        public string? AddressLine { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Account Account { get; set; } = null!;
    }
}
