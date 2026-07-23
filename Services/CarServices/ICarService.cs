using E_Commerce_API.Dtos.CarDtos;

namespace E_Commerce_API.Services.CarServices
{
    public interface ICarService
    {
        Task CreateCarItem(CreateCartItemDto dto, int userId);

        Task<CartDto> GetUserCart(int userId);

        Task DeleteCarItem(CartItem cartItem);

        Task<CartItem?> GetCartItem(int userId, int productId);

        Task UpdateCarItemQuantity(CartItem cartItem);

        Task DeleteUserCartItems(int userId);

    }
}
