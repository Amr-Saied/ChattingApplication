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
        private readonly IPhotoService _photoService;

        public UsersController(
            IUserService userService,
            IWebHostEnvironment env,
            IPhotoService photoService
        )
        {
            _userService = userService;
            _env = env;
            _photoService = photoService;
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

            var url = await _photoService.UploadPhotoAsync(file);
            if (string.IsNullOrEmpty(url))
                return BadRequest("Photo upload failed.");

            return Ok(new { url });
        }

        [HttpPost("AddPhoto/{userId}")]
        public async Task<IActionResult> AddPhoto(int userId, [FromBody] PhotoDTO photo)
        {
            var result = await _userService.AddPhotoToGallery(userId, photo);
            if (!result)
                return BadRequest("Could not add photo to gallery.");
            return Ok(new { success = true });
        }

        [HttpDelete("DeletePhoto/{userId}/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int userId, int photoId)
        {
            var result = await _userService.DeletePhotoFromGallery(userId, photoId);
            if (!result)
                return BadRequest("Could not delete photo from gallery.");
            return Ok(new { success = true });
        }
    }
}
