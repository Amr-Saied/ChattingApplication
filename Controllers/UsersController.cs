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
        private readonly IWebHostEnvironment _env;

        public UsersController(IUserService userService, IWebHostEnvironment env)
        {
            _userService = userService;
            _env = env;
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

        [HttpPut("UpdateUser/{id}")]
        public async Task<ActionResult<MemeberDTO>> UpdateUser(int id, MemeberDTO user)
        {
            return Ok(await _userService.UpdateUserDTO(id, user));
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
            return Ok(new { url });
        }
    }
}
