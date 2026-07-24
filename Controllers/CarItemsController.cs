using E_Commerce_API.Dtos.CarDtos;
using E_Commerce_API.Services.CarServices;
using E_Commerce_API.Services.ProductService;
using E_Commerce_API.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarItemsController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ICarService _carService;
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public CarItemsController(IUserService userService, ICarService carService, IProductService productService, IMapper mapper)
        {
            _userService = userService;
            _carService = carService;
            _productService = productService;
            _mapper = mapper;
        }

        /// <summary>
        /// Customer فقط - إضافة منتج للكارت
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> AddItemForCar([FromBody] CreateCartItemDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _userService.GetUserById(userId);
                if (user == null) return NotFound("User not found");

                var product = await _productService.GetById(dto.ProductId);
                if (product == null) return NotFound("Product not found");

                if (dto.Quantity > product.StockQuantity)
                    return BadRequest($"الكمية المطلوبة أكبر من المتاح ({product.StockQuantity})");

                await _carService.CreateCarItem(dto, userId);
                return Ok("Add Succeeded");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Customer فقط - عرض الكارت
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet("MyCart")]
        public async Task<IActionResult> GetCartAsync()
        {
            var userId = GetCurrentUserId();
            var cart = await _carService.GetUserCart(userId);
            return Ok(cart);
        }

        /// <summary>
        /// Customer فقط - حذف منتج من الكارت
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpDelete]
        public async Task<IActionResult> DeleteItemFromCart(int ProductId)
        {
            var userId = GetCurrentUserId();
            var item = await _carService.GetCartItem(userId, ProductId);
            if (item == null)
                return NotFound("Item not found in cart");
            await _carService.DeleteCarItem(item);
            return NoContent();
        }

        /// <summary>
        /// Customer فقط - تعديل كمية منتج في الكارت
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpPut]
        public async Task<IActionResult> UpdateCartQuantity([FromBody] UpdateCartDto dto)
        {
            var userId = GetCurrentUserId();
            var product = await _productService.GetById(dto.ProductId);
            if (product == null)
                return NotFound("Not Product Found With This Id");
            var item = await _carService.GetCartItem(userId, dto.ProductId);
            if (item == null)
                return BadRequest("This product is not in the basket.");
            if (dto.Quantity > product.StockQuantity)
                return BadRequest("The quantity requested exceeds the available stock.");
            _mapper.Map(dto, item);
            await _carService.UpdateCarItemQuantity(item);
            return Ok("Cart Updated Successfully");
        }
    }
}