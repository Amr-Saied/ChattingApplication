using System.Collections.Generic;
using System.Threading.Tasks;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChattingApplicationProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpGet("GetUsers")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsersAsync()
        {
            return Ok(await _userService.GetUsers());
        }

        [Authorize]
        [HttpGet("GetUserById/{id}")]
        public async Task<ActionResult<AppUser>> GetUserById(int id)
        {
            return Ok(await _userService.GetUserById(id));
        }
    }
}
