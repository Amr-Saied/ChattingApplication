using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChattingApplicationProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("GetAllUsers")]
        public async Task<ActionResult<PagedResult<AdminUserResponseDTO>>> GetAllUsers(
            [FromQuery] PaginationParams paginationParams
        )
        {
            try
            {
                var users = await _adminService.GetAllUsersForAdminAsync(paginationParams);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving users: {ex.Message}");
            }
        }

        [HttpGet("SearchUsers")]
        public async Task<ActionResult<IEnumerable<AdminUserResponseDTO>>> SearchUsers(
            [FromQuery] string searchTerm
        )
        {
            try
            {
                var users = await _adminService.SearchUsersForAdminAsync(searchTerm);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error searching users: {ex.Message}");
            }
        }

        [HttpGet("GetUser/{userId}")]
        public async Task<ActionResult<AdminUserResponseDTO>> GetUser(int userId)
        {
            try
            {
                var user = await _adminService.GetUserForAdminAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving user: {ex.Message}");
            }
        }

        [HttpPut("EditUser/{userId}")]
        public async Task<ActionResult<AdminUserResponseDTO>> EditUser(
            int userId,
            [FromBody] AdminEditUserDTO editUserDto
        )
        {
            try
            {
                if (editUserDto == null)
                    return BadRequest("Invalid user data");

                var updatedUser = await _adminService.EditUserAsync(userId, editUserDto);
                if (updatedUser == null)
                    return NotFound("User not found or update failed");

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating user: {ex.Message}");
            }
        }

        [HttpPost("BanUser/{userId}")]
        public async Task<ActionResult> BanUser(int userId, [FromBody] AdminBanUserDTO banDto)
        {
            try
            {
                if (banDto == null)
                    return BadRequest("Invalid ban data");

                // Validate ban data
                if (!banDto.IsPermanentBan && !banDto.BanExpiryDate.HasValue)
                    return BadRequest("Temporary ban must have an expiry date");

                if (banDto.IsPermanentBan && banDto.BanExpiryDate.HasValue)
                    return BadRequest("Permanent ban should not have an expiry date");

                var result = await _adminService.BanUserAsync(userId, banDto);
                if (!result)
                    return NotFound("User not found or ban failed");

                return Ok(new { message = "User banned successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error banning user: {ex.Message}");
            }
        }

        [HttpPost("UnbanUser/{userId}")]
        public async Task<ActionResult> UnbanUser(int userId)
        {
            try
            {
                var result = await _adminService.UnbanUserAsync(userId);
                if (!result)
                    return NotFound("User not found or unban failed");

                return Ok(new { message = "User unbanned successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error unbanning user: {ex.Message}");
            }
        }

        [HttpDelete("DeleteUser/{userId}")]
        public async Task<ActionResult> DeleteUser(int userId)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(userId);
                if (!result)
                    return NotFound("User not found or deletion failed");

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting user: {ex.Message}");
            }
        }

        [HttpGet("CheckUserBanStatus/{userId}")]
        public async Task<ActionResult> CheckUserBanStatus(int userId)
        {
            try
            {
                var isBanned = await _adminService.IsUserBannedAsync(userId);
                return Ok(new { userId, isBanned });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking ban status: {ex.Message}");
            }
        }

        [HttpPost("RefreshBanStatus")]
        public ActionResult RefreshBanStatus()
        {
            try
            {
                _adminService.CheckAndUnbanExpiredUsers();
                return Ok(new { message = "Ban status refreshed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error refreshing ban status: {ex.Message}");
            }
        }
    }
}
