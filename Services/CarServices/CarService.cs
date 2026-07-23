using E_Commerce_API.Dtos.CarDtos;

namespace E_Commerce_API.Services.CarServices
{
    public class CarService : ICarService
    {
        private readonly AppDbContext _context;

        public CarService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateCarItem(CreateCartItemDto dto, int userId)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);

            var existingItem = await _context.CartItems.FirstOrDefaultAsync(
                s => s.UserId == userId && s.ProductId == dto.ProductId
            );

            int currentCartQty = existingItem?.Quantity ?? 0;
            int totalRequested = currentCartQty + dto.Quantity;

            // تحقق إن الكمية المطلوبة مش أكبر من المخزون
            if (totalRequested > product.StockQuantity)
                throw new InvalidOperationException($"الكمية المتاحة {product.StockQuantity} فقط، وعندك {currentCartQty} في السلة");

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UserId = userId,
                };
                await _context.CartItems.AddAsync(newItem);
            }
            await _context.SaveChangesAsync();
        }

        public async Task  DeleteCarItem(CartItem cartItem)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
        }

        public async Task<CartDto> GetUserCart(int userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var warnings = new List<string>();

            if (!cartItems.Any())
                return new CartDto { Items = new List<CartItemDto>(), TotalPrice = 0 };

            // تحقق من الكميات وعدلها
            foreach (var item in cartItems)
            {
                if (item.Quantity > item.Product.StockQuantity)
                {
                    if (item.Product.StockQuantity == 0)
                    {
                        warnings.Add($"{item.Product.Name} نفذت الكمية وتم إزالته من السلة");
                        _context.CartItems.Remove(item);
                    }
                    else
                    {
                        warnings.Add($"{item.Product.Name} الكمية المتاحة {item.Product.StockQuantity} فقط، تم تعديل طلبك");
                        item.Quantity = item.Product.StockQuantity;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var updatedItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var items = updatedItems.Select(c => new CartItemDto
            {
                ProductId = c.ProductId,
                ProductName = c.Product.Name,
                Price = c.Product.Price,
                Quantity = c.Quantity,
                ItemTotal = c.Quantity * c.Product.Price
            }).ToList();

            return new CartDto
            {
                Items = items,
                TotalPrice = items.Sum(x => x.ItemTotal),
                Warnings = warnings
            };
        }

        public async Task<CartItem?> GetCartItem(int userId, int productId)
        {
            return await _context.CartItems
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.ProductId == productId);
        }

        public async Task UpdateCarItemQuantity(CartItem cartItem)
        {
            _context.CartItems.Update(cartItem);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserCartItems(int userId)
        {
            var items = await _context.CartItems
            .Where(c => c.UserId == userId)
            .ToListAsync();

            _context.CartItems.RemoveRange(items);

            await _context.SaveChangesAsync();
        }

    }
}
