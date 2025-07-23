using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChattingApplicationProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetUsers")]
        public async Task<ActionResult<IEnumerable<MemeberDTO>>> GetUsersAsync()
        {
            return Ok(await _userService.GetUsersDTO());
        }

        [HttpGet("GetUserById/{id}")]
        public async Task<ActionResult<MemeberDTO>> GetUserById(int id)
        {
            return Ok(await _userService.GetUserByIdDTO(id));
        }

        [HttpGet("GetUserByUsername/{username}")]
        public async Task<ActionResult<MemeberDTO>> GetUserByUsername(string username)
        {
            return Ok(await _userService.GetUserByUsernameDTO(username));
        }
    }
}
