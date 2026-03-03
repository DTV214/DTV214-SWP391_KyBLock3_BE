using System.Globalization;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Interfaces;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Services
{
    public class DirectionsService : IDirectionsService
    {
        private readonly IUnitOfWork _uow;

        public DirectionsService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<DirectionsResponse> BuildDirectionsUrlAsync(int storeLocationId, DirectionsRequest req)
        {
            if (storeLocationId <= 0) throw new Exception("storeLocationId is required.");

            var storeRepo = _uow.GetRepository<StoreLocation>();
            var store = await storeRepo.GetByIdAsync(storeLocationId);
            if (store == null || store.IsActive == false) throw new Exception("Store location not found.");

            var mode = NormalizeTravelMode(req.TravelMode);

            // Google Maps Directions URL (không cần API key)
            // https://www.google.com/maps/dir/?api=1&origin=lat,lng&destination=lat,lng&travelmode=driving
            var origin = $"{req.FromLat.ToString(CultureInfo.InvariantCulture)},{req.FromLng.ToString(CultureInfo.InvariantCulture)}";
            var dest = $"{store.Latitude.ToString(CultureInfo.InvariantCulture)},{store.Longitude.ToString(CultureInfo.InvariantCulture)}";

            var url =
                $"https://www.google.com/maps/dir/?api=1" +
                $"&origin={Uri.EscapeDataString(origin)}" +
                $"&destination={Uri.EscapeDataString(dest)}" +
                $"&travelmode={Uri.EscapeDataString(mode)}";

            return new DirectionsResponse
            {
                StoreLocationId = store.StoreLocationId,
                Url = url
            };
        }

        private static string NormalizeTravelMode(string? mode)
        {
            mode = (mode ?? "driving").Trim().ToLowerInvariant();
            return mode switch
            {
                "driving" or "walking" or "bicycling" or "transit" => mode,
                _ => "driving"
            };
        }
    }
}
