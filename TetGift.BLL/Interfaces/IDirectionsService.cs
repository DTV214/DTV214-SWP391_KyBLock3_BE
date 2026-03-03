using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IDirectionsService
    {
        Task<DirectionsResponse> BuildDirectionsUrlAsync(int storeLocationId, DirectionsRequest req);
    }
}
