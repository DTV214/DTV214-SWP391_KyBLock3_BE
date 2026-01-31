using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface ICartService
{
    Task<CartResponseDto> GetCartByAccountIdAsync(int accountId);
    Task<CartResponseDto> AddItemAsync(int accountId, AddToCartRequest request);
    Task<CartResponseDto> UpdateItemAsync(int cartDetailId, UpdateCartItemRequest request);
    Task<CartResponseDto> RemoveItemAsync(int cartDetailId);
    Task<CartResponseDto> ApplyPromotionAsync(int accountId, ApplyPromotionRequest request);
    Task<int> GetCartItemCountAsync(int accountId);
    Task ClearCartAsync(int accountId);
    Task ValidateCartDetailOwnershipAsync(int cartDetailId, int accountId);
}
