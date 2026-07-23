using E_Commerce_API.Dtos.OrdersDto;
using E_Commerce_API.Services.OrderService;
using E_Commerce_API.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService, IUserService userService)
        {
            _orderService = orderService;
            _userService = userService;
        }

        /// <summary>
        /// Customer فقط - إنشاء أوردر جديد
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder()
        {
            var userId = GetCurrentUserId();
            var (order, error) = await _orderService.CreateOrder(userId);

            if (error != null)
                return BadRequest(error);

            if (order == null)
                return BadRequest("حصل خطأ في إنشاء الطلب");

            return Ok(new
            {
                orderId = order.Id,
                totalPrice = order.TotalPrice,
                orderStatus = order.OrderStatus
            });
        }

        /// <summary>
        /// Customer فقط - عرض كل أوردراتي
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet("MyOrders")]
        public async Task<IActionResult> GetAllUserOrders()
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetAllUserOrders(userId);
            return Ok(orders);
        }

        /// <summary>
        /// Customer فقط - عرض تفاصيل أوردر
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet("orderdetails/{orderId}")]
        public async Task<IActionResult> OrderDetailsAsync(int orderId)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                return NotFound("Not Order Found With This Id");
            if (order.UserId != userId)
                return Forbid();
            var orderDetails = await _orderService.GetOrderDetails(orderId);
            return Ok(orderDetails);
        }

        /// <summary>
        /// Admin فقط - تعديل حالة الأوردر
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateStatus/{orderId}")]
        public async Task<IActionResult> UpdateOraderStatus(int orderId, [FromForm] UpdateOrderStatusDto dto)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                return NotFound("Not Order Found With This Id");
            order.OrderStatus = dto.OrderStatus;
            await _orderService.UpdateStatus();
            return Ok("Order Status Updated Successfully");
        }

        /// <summary>
        /// Customer فقط - إلغاء أوردر
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpPut("CancelOrder/{orderid}")]
        public async Task<IActionResult> CancelOrderAsync(int orderid)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetOrderById(orderid);
            if (order == null)
                return NotFound("Not Order Found With This Id");
            if (order.UserId != userId)
                return Forbid();
            var result = await _orderService.CancelOrder(orderid);
            if (result != "Success")
                return BadRequest(result);
            return Ok("Order Cancelled Successfully");
        }
    }
}