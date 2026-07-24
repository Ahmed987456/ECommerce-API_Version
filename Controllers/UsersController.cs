using E_Commerce_API.Dtos.UserDtos;
using E_Commerce_API.Enums;
using E_Commerce_API.Models;
using E_Commerce_API.Services.CarServices;
using E_Commerce_API.Services.OrderService;
using E_Commerce_API.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;
        private readonly ICarService _carService;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IMapper mapper, IOrderService orderService, ICarService carService)
        {
            _userService = userService;
            _mapper = mapper;
            _orderService = orderService;
            _carService = carService;
        }

        /// <summary>
        /// متاح للكل - تسجيل يوزر جديد
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatUserAsync([FromBody] CreateUserDto dto)
        {
            var exisitEmail = await _userService.Emailconfirmation(dto.Email);
            if (exisitEmail)
                return BadRequest("The email already exists");
            var user = _mapper.Map<User>(dto);
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            await _userService.CreateUser(user);
            return Ok(_mapper.Map<UserDto>(user));
        }

        /// <summary>
        /// Admin فقط - عرض كل اليوزرز
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsersAsync()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }

        /// <summary>
        /// Admin يشوف أي يوزر - Customer يشوف بياناته هو بس
        /// </summary>
        [Authorize]
        [HttpGet("{UserId}")]
        public async Task<IActionResult> GetUserByIdAsync(int UserId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole != "Admin" && currentUserId != UserId)
                return Forbid();

            var user = await _userService.GetUserById(UserId);
            if (user == null)
                return NotFound("Not User Found With This Id");
            return Ok(_mapper.Map<UserDto>(user));
        }

        /// <summary>
        /// Admin يعدل أي يوزر - Customer يعدل بياناته هو بس
        /// </summary>
        [Authorize]
        [HttpPut("{UserId}")]
        public async Task<IActionResult> UpdateUser(int UserId, [FromBody] UpdateUserDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserRole != "Admin" && currentUserId != UserId)
                return Forbid();

            var user = await _userService.GetUserById(UserId);
            if (user == null)
                return NotFound("Not User Found With This Id");
            if (!string.IsNullOrEmpty(dto.Email))
            {
                var emailExists = await _userService.EmailExistsForUpdate(dto.Email, UserId);
                if (emailExists)
                    return BadRequest("Email already exists");
            }
            _mapper.Map(dto, user);
            await _userService.UpdateUser(user);
            return Ok("Modified successfully");
        }

        /// <summary>
        /// Admin فقط - تغيير Role يوزر
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{UserId}/role")]
        public async Task<IActionResult> ChangeUserRole(int UserId, [FromBody] UserRole role)
        {
            var user = await _userService.GetUserById(UserId);
            if (user == null)
                return NotFound("Not User Found With This Id");
            user.Role = role;
            await _userService.UpdateUser(user);
            return Ok("Role Updated Successfully");
        }

        /// <summary>
        /// Admin فقط - حذف يوزر
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{UserId}")]
        public async Task<IActionResult> DeleteUserAsync(int UserId)
        {
            var user = await _userService.GetUserById(UserId);
            if (user == null)
                return NotFound("Not User Found With This Id");
            var order = await _orderService.HasOrders(UserId);
            if (order)
                return BadRequest("Cannot delete user because he has orders");
            await _carService.DeleteUserCartItems(UserId);
            await _userService.DeleteUser(user);
            return NoContent();
        }
    }
}