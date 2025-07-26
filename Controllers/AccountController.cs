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

        public AccountController(
            IUserService userService,
            ITokenService tokenService,
            IMapper mapper
        )
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDto)
        {
            if (await _userService.UserExists(registerDto.Username))
                return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();

            // Use AutoMapper to map RegisterDTO to AppUser
            var user = _mapper.Map<AppUser>(registerDto);

            // Set password hash and salt
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
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
            var user = await _userService.GetUserByUsername(loginDto.Username);
            if (user == null)
                return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Invalid password");
            }

            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenService.CreateToken(user),
                Role = user.Role
            };
        }
    }
}
