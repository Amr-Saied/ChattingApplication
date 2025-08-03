using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChattingApplicationProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly IAdminService _adminService;

        public AccountController(
            IUserService userService,
            ITokenService tokenService,
            IMapper mapper,
            IAdminService adminService
        )
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
            _adminService = adminService;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDto)
        {
            if (await _userService.UserExists(registerDto.Username ?? string.Empty))
                return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();

            // Use AutoMapper to map RegisterDTO to AppUser
            var user = _mapper.Map<AppUser>(registerDto);

            // Set password hash and salt
            user.PasswordHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(registerDto.Password ?? string.Empty)
            );
            user.PasswordSalt = hmac.Key;

            await _userService.AddUser(user);

            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDto)
        {
            var user = await _userService.GetUserByUsername(loginDto.Username ?? string.Empty);
            if (user == null)
                return Unauthorized("Invalid username");

            if (user.PasswordSalt == null)
                return Unauthorized("Invalid user data");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(loginDto.Password ?? string.Empty)
            );

            if (user.PasswordHash == null)
                return Unauthorized("Invalid user data");

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Invalid password");
            }

            // Check if user is banned - this will automatically unban expired users
            var isBanned = await _adminService.IsUserBannedAsync(user.Id);
            if (isBanned)
            {
                // Get updated user details after potential unban
                var userDetails = await _adminService.GetUserForAdminAsync(user.Id);
                if (userDetails != null && userDetails.IsBanned)
                {
                    var banMessage = "Your account has been banned.";
                    if (!string.IsNullOrEmpty(userDetails.BanReason))
                    {
                        banMessage += $" Reason: {userDetails.BanReason}";
                    }
                    if (!userDetails.IsPermanentBan && userDetails.BanExpiryDate.HasValue)
                    {
                        banMessage +=
                            $" Your ban expires on: {userDetails.BanExpiryDate.Value.ToString("MM/dd/yyyy hh:mm tt")}";
                    }

                    banMessage += " Please contact an administrator for more information.";
                    return BadRequest(
                        new
                        {
                            error = "USER_BANNED",
                            message = banMessage,
                            isPermanentBan = userDetails.IsPermanentBan,
                            banExpiryDate = userDetails.BanExpiryDate,
                            banReason = userDetails.BanReason
                        }
                    );
                }
            }

            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                Role = user.Role
            };
        }

        [HttpGet("CheckBanStatus/{userId}")]
        public async Task<ActionResult> CheckCurrentUserBanStatus(int userId)
        {
            try
            {
                var user = await _adminService.GetUserForAdminAsync(userId);
                if (user == null)
                {
                    return Ok(new { userId, isBanned = false });
                }

                return Ok(
                    new
                    {
                        userId,
                        isBanned = user.IsBanned,
                        banReason = user.BanReason,
                        isPermanentBan = user.IsPermanentBan,
                        banExpiryDate = user.BanExpiryDate.HasValue ? user.BanExpiryDate.Value.ToString("MM/dd/yyyy hh:mm tt") : null
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking ban status: {ex.Message}");
            }
        }
    }
}
