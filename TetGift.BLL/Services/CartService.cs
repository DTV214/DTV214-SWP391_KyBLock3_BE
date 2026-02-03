using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;
    private readonly IPromotionService _promotionService;

    public CartService(IUnitOfWork uow, IPromotionService promotionService)
    {
        _uow = uow;
        _promotionService = promotionService;
    }

    public async Task<CartResponseDto> GetCartByAccountIdAsync(int accountId)
    {
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Accountid == accountId,
            include: q => q
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
        );

        if (cart == null)
        {
            // Tạo cart mới nếu chưa có
            var newCart = new Cart
            {
                Accountid = accountId,
                Totalprice = 0
            };
            await cartRepo.AddAsync(newCart);
            await _uow.SaveAsync();

            return new CartResponseDto
            {
                CartId = newCart.Cartid,
                AccountId = accountId,
                Items = new List<CartItemResponseDto>(),
                TotalPrice = 0,
                FinalPrice = 0,
                ItemCount = 0
            };
        }

        return MapToCartResponseDto(cart);
    }

    public async Task<CartResponseDto> AddItemAsync(int accountId, AddToCartRequest request)
    {
        // Validate Product
        var productRepo = _uow.GetRepository<Product>();
        var product = await productRepo.GetByIdAsync(request.ProductId);
        if (product == null)
            throw new Exception("Sản phẩm không tồn tại.");

        if (product.Status != ProductStatus.ACTIVE)
            throw new Exception("Sản phẩm không khả dụng.");

        if (product.Price == null || product.Price <= 0)
            throw new Exception("Giá sản phẩm không hợp lệ.");

        // Lấy hoặc tạo Cart
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Accountid == accountId,
            include: q => q.Include(c => c.CartDetails)
        );

        if (cart == null)
        {
            cart = new Cart
            {
                Accountid = accountId,
                Totalprice = 0
            };
            await cartRepo.AddAsync(cart);
            await _uow.SaveAsync();
        }

        // Kiểm tra sản phẩm đã có trong cart chưa
        var cartDetailRepo = _uow.GetRepository<CartDetail>();
        var existingDetail = await cartDetailRepo.FindAsync(
            cd => cd.Cartid == cart.Cartid && cd.Productid == request.ProductId,
            include: null
        );

        if (existingDetail != null)
        {
            // Cộng dồn quantity
            existingDetail.Quantity = (existingDetail.Quantity ?? 0) + request.Quantity;
            cartDetailRepo.Update(existingDetail);
        }
        else
        {
            // Tạo mới CartDetail
            var newDetail = new CartDetail
            {
                Cartid = cart.Cartid,
                Productid = request.ProductId,
                Quantity = request.Quantity
            };
            await cartDetailRepo.AddAsync(newDetail);
        }

        // Tính lại Totalprice
        await RecalculateCartTotalAsync(cart.Cartid);
        await _uow.SaveAsync();

        // Load lại cart với đầy đủ thông tin
        var updatedCart = await cartRepo.FindAsync(
            c => c.Cartid == cart.Cartid,
            include: q => q
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
        );

        return MapToCartResponseDto(updatedCart!);
    }

    public async Task<CartResponseDto> UpdateItemAsync(int cartDetailId, UpdateCartItemRequest request)
    {
        var cartDetailRepo = _uow.GetRepository<CartDetail>();
        var cartDetail = await cartDetailRepo.FindAsync(
            cd => cd.Cartdetailid == cartDetailId,
            include: q => q.Include(cd => cd.Cart)
        );

        if (cartDetail == null)
            throw new Exception("Không tìm thấy sản phẩm trong giỏ hàng.");

        // Cập nhật quantity
        cartDetail.Quantity = request.Quantity;
        cartDetailRepo.Update(cartDetail);

        // Tính lại Totalprice
        await RecalculateCartTotalAsync(cartDetail.Cart!.Cartid);
        await _uow.SaveAsync();

        // Load lại cart với đầy đủ thông tin
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Cartid == cartDetail.Cart.Cartid,
            include: q => q
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
        );

        return MapToCartResponseDto(cart!);
    }

    public async Task ValidateCartDetailOwnershipAsync(int cartDetailId, int accountId)
    {
        var cartDetailRepo = _uow.GetRepository<CartDetail>();
        var cartDetail = await cartDetailRepo.FindAsync(
            cd => cd.Cartdetailid == cartDetailId,
            include: q => q.Include(cd => cd.Cart)
        );

        if (cartDetail == null)
            throw new Exception("Không tìm thấy sản phẩm trong giỏ hàng.");

        if (cartDetail.Cart?.Accountid != accountId)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác với item này.");
    }

    public async Task<CartResponseDto> RemoveItemAsync(int cartDetailId)
    {
        var cartDetailRepo = _uow.GetRepository<CartDetail>();
        var cartDetail = await cartDetailRepo.FindAsync(
            cd => cd.Cartdetailid == cartDetailId,
            include: q => q.Include(cd => cd.Cart)
        );

        if (cartDetail == null)
            throw new Exception("Không tìm thấy sản phẩm trong giỏ hàng.");

        var cartId = cartDetail.Cart!.Cartid;
        cartDetailRepo.Delete(cartDetail);

        // Tính lại Totalprice
        await RecalculateCartTotalAsync(cartId);
        await _uow.SaveAsync();

        // Load lại cart với đầy đủ thông tin
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Cartid == cartId,
            include: q => q
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
        );

        return MapToCartResponseDto(cart!);
    }

    public async Task<CartResponseDto> ApplyPromotionAsync(int accountId, ApplyPromotionRequest request)
    {
        // Lấy cart từ database
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Accountid == accountId,
            include: q => q
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
        );

        if (cart == null)
            throw new Exception("Giỏ hàng không tồn tại.");

        var cartDto = MapToCartResponseDto(cart);
        if (cartDto.ItemCount == 0)
            throw new Exception("Giỏ hàng trống, không thể áp dụng mã giảm giá.");

        // Validate promotion code bằng cách sử dụng PromotionService (kế thừa code cũ)
        var allPromotions = await _promotionService.GetAllAsync();
        var promotion = allPromotions.FirstOrDefault(p => 
            p.Code.Equals(request.PromotionCode, StringComparison.OrdinalIgnoreCase) && 
            p.IsActive);

        if (promotion == null)
            throw new Exception("Mã giảm giá không hợp lệ hoặc đã hết hạn.");

        // Tính final price với discount
        var finalPrice = cartDto.TotalPrice - promotion.DiscountValue;
        if (finalPrice < 0) finalPrice = 0;

        cartDto.DiscountValue = promotion.DiscountValue;
        cartDto.FinalPrice = finalPrice;
        cartDto.PromotionCode = promotion.Code;

        return cartDto;
    }

    public async Task<int> GetCartItemCountAsync(int accountId)
    {
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Accountid == accountId,
            include: q => q.Include(c => c.CartDetails)
        );

        if (cart == null || cart.CartDetails == null)
            return 0;

        return cart.CartDetails.Count;
    }

    public async Task ClearCartAsync(int accountId)
    {
        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.FindAsync(
            c => c.Accountid == accountId,
            include: q => q.Include(c => c.CartDetails)
        );

        if (cart != null && cart.CartDetails != null && cart.CartDetails.Any())
        {
            var cartDetailRepo = _uow.GetRepository<CartDetail>();
            cartDetailRepo.DeleteRange(cart.CartDetails);
            cart.Totalprice = 0;
            cartRepo.Update(cart);
            await _uow.SaveAsync();
        }
    }

    private async Task RecalculateCartTotalAsync(int cartId)
    {
        var cartDetailRepo = _uow.GetRepository<CartDetail>();
        var cartDetails = await cartDetailRepo.GetAllAsync(
            cd => cd.Cartid == cartId,
            include: q => q.Include(cd => cd.Product)
        );

        decimal total = 0;
        foreach (var detail in cartDetails)
        {
            if (detail.Product != null && detail.Product.Price.HasValue && detail.Quantity.HasValue)
            {
                total += detail.Product.Price.Value * detail.Quantity.Value;
            }
        }

        var cartRepo = _uow.GetRepository<Cart>();
        var cart = await cartRepo.GetByIdAsync(cartId);
        if (cart != null)
        {
            cart.Totalprice = total;
            cartRepo.Update(cart);
        }
    }

    private CartResponseDto MapToCartResponseDto(Cart cart)
    {
        var items = new List<CartItemResponseDto>();
        decimal totalPrice = 0;

        if (cart.CartDetails != null)
        {
            foreach (var detail in cart.CartDetails)
            {
                if (detail.Product != null)
                {
                    var price = detail.Product.Price ?? 0;
                    var quantity = detail.Quantity ?? 0;
                    var subTotal = price * quantity;

                    items.Add(new CartItemResponseDto
                    {
                        CartDetailId = detail.Cartdetailid,
                        ProductId = detail.Product.Productid,
                        ProductName = detail.Product.Productname,
                        Sku = detail.Product.Sku,
                        Price = price,
                        Quantity = quantity,
                        SubTotal = subTotal,
                        ImageUrl = detail.Product.ImageUrl
                    });

                    totalPrice += subTotal;
                }
            }
        }

        return new CartResponseDto
        {
            CartId = cart.Cartid,
            AccountId = cart.Accountid ?? 0,
            Items = items,
            TotalPrice = totalPrice,
            FinalPrice = totalPrice, // Sẽ được cập nhật khi apply promotion
            ItemCount = items.Count
        };
    }
}
