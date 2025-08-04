using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using ChattingApplicationProject.DTO;
using ChattingApplicationProject.Interfaces;
using ChattingApplicationProject.Models;
using ChattingApplicationProject.Services;
using Google.Apis.Auth;
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
        private readonly IEmailService _emailService;

        public AccountController(
            IUserService userService,
            ITokenService tokenService,
            IMapper mapper,
            IAdminService adminService,
            IEmailService emailService
        )
        {
            _userService = userService;
            _tokenService = tokenService;
            _mapper = mapper;
            _adminService = adminService;
            _emailService = emailService;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDto)
        {
            if (await _userService.UserExists(registerDto.Username ?? string.Empty))
                return BadRequest("Username is taken");

            if (await _emailService.EmailExists(registerDto.Email ?? string.Empty))
                return BadRequest("Email is already registered");

            using var hmac = new HMACSHA512();

            // Use AutoMapper to map RegisterDTO to AppUser
            var user = _mapper.Map<AppUser>(registerDto);

            // Set password hash and salt
            user.PasswordHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(registerDto.Password ?? string.Empty)
            );
            user.PasswordSalt = hmac.Key;

            // Generate email confirmation token
            user.EmailConfirmationToken = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)
            );
            user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _userService.AddUser(user);

            // Send confirmation email
            var confirmationLink =
                $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail?token={user.EmailConfirmationToken}";
            await _emailService.SendEmailConfirmationAsync(
                user.Email!,
                user.UserName!,
                confirmationLink
            );

            return Ok(
                new
                {
                    message = "Registration successful. Please check your email to confirm your account."
                }
            );
        }

        [HttpPost("Login")]
        public async Task<ActionResult<object>> Login(LoginDTO loginDto)
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

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                return BadRequest(
                    new
                    {
                        error = "EMAIL_NOT_CONFIRMED",
                        message = "Please confirm your email address before logging in. Check your inbox for the confirmation link.",
                        email = user.Email
                    }
                );
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
                            banExpiryDate = userDetails.BanExpiryDate.HasValue
                                ? userDetails.BanExpiryDate.Value.ToString("MM/dd/yyyy hh:mm tt")
                                : null,
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

                // Build complete ban message (same format as login response)
                var banMessage = "Your account has been banned.";
                if (!string.IsNullOrEmpty(user.BanReason))
                {
                    banMessage += $" Reason: {user.BanReason}";
                }
                if (!user.IsPermanentBan && user.BanExpiryDate.HasValue)
                {
                    banMessage +=
                        $" Your ban expires on: {user.BanExpiryDate.Value.ToString("MM/dd/yyyy hh:mm tt")}";
                }
                banMessage += " Please contact an administrator for more information.";

                return Ok(
                    new
                    {
                        userId,
                        isBanned = user.IsBanned,
                        message = banMessage,
                        banReason = user.BanReason,
                        isPermanentBan = user.IsPermanentBan,
                        banExpiryDate = user.BanExpiryDate.HasValue
                            ? user.BanExpiryDate.Value.ToString("MM/dd/yyyy hh:mm tt")
                            : null
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking ban status: {ex.Message}");
            }
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            var user = await _emailService.GetUserByEmailConfirmationToken(token);

            if (user == null)
                return BadRequest("Invalid confirmation token");

            if (user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
                return BadRequest("Confirmation token has expired");

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;

            await _emailService.UpdateUser(user);

            return Ok(new { message = "Email confirmed successfully!" });
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO forgotPasswordDto)
        {
            var user = await _emailService.GetUserByEmail(forgotPasswordDto.Email ?? string.Empty);

            if (user == null)
                return Ok(
                    new
                    {
                        message = "If an account with this email exists, a password reset link has been sent."
                    }
                );

            // Generate password reset token
            user.PasswordResetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _emailService.UpdateUser(user);

            // Send password reset email
            var resetLink =
                $"{Request.Scheme}://{Request.Host}/reset-password?token={user.PasswordResetToken}";
            await _emailService.SendPasswordResetAsync(user.Email!, user.UserName!, resetLink);

            return Ok(
                new
                {
                    message = "If an account with this email exists, a password reset link has been sent."
                }
            );
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPasswordDto)
        {
            var user = await _emailService.GetUserByPasswordResetToken(
                resetPasswordDto.Token ?? string.Empty
            );

            if (user == null)
                return BadRequest("Invalid reset token");

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return BadRequest("Reset token has expired");

            // Update password
            using var hmac = new HMACSHA512();
            user.PasswordHash = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(resetPasswordDto.NewPassword ?? string.Empty)
            );
            user.PasswordSalt = hmac.Key;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _emailService.UpdateUser(user);

            return Ok(new { message = "Password reset successfully!" });
        }

        [HttpPost("ForgotUsername")]
        public async Task<IActionResult> ForgotUsername(ForgotUsernameDTO forgotUsernameDto)
        {
            var user = await _emailService.GetUserByEmail(forgotUsernameDto.Email ?? string.Empty);

            if (user == null)
                return Ok(
                    new
                    {
                        message = "If an account with this email exists, a username reminder has been sent."
                    }
                );

            // Send username reminder email
            await _emailService.SendUsernameReminderAsync(user.Email!, user.UserName!);

            return Ok(
                new
                {
                    message = "If an account with this email exists, a username reminder has been sent."
                }
            );
        }

        [HttpPost("ResendConfirmation")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] string email)
        {
            var user = await _emailService.GetUserByEmail(email);

            if (user == null)
                return Ok(
                    new
                    {
                        message = "If an account with this email exists, a confirmation email has been sent."
                    }
                );

            if (user.EmailConfirmed)
                return Ok(new { message = "Your email is already confirmed." });

            // Generate new confirmation token
            user.EmailConfirmationToken = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32)
            );
            user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _emailService.UpdateUser(user);

            // Send new confirmation email
            var confirmationLink =
                $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail?token={user.EmailConfirmationToken}";
            await _emailService.SendEmailConfirmationAsync(
                user.Email!,
                user.UserName!,
                confirmationLink
            );

            return Ok(
                new
                {
                    message = "If an account with this email exists, a new confirmation email has been sent."
                }
            );
        }

        [HttpPost("GoogleLogin")]
        public async Task<ActionResult<object>> GoogleLogin(GoogleLoginDTO googleLoginDto)
        {
            try
            {
                // Check if user exists with this Google ID
                var existingUser = await _userService.GetUserByGoogleId(googleLoginDto.GoogleId);

                if (existingUser != null)
                {
                    // User exists, check if banned
                    var isBanned = await _adminService.IsUserBannedAsync(existingUser.Id);
                    if (isBanned)
                    {
                        var userDetails = await _adminService.GetUserForAdminAsync(existingUser.Id);
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
                                    banExpiryDate = userDetails.BanExpiryDate.HasValue
                                        ? userDetails.BanExpiryDate.Value.ToString(
                                            "MM/dd/yyyy hh:mm tt"
                                        )
                                        : null,
                                    banReason = userDetails.BanReason
                                }
                            );
                        }
                    }

                    return new UserDTO
                    {
                        Username = existingUser.UserName,
                        Token = _tokenService.CreateToken(existingUser),
                        Role = existingUser.Role
                    };
                }

                // User doesn't exist, create new user
                var newUser = new AppUser
                {
                    UserName =
                        googleLoginDto.Email?.Split('@')[0]
                        + "_"
                        + Guid.NewGuid().ToString().Substring(0, 8),
                    Email = googleLoginDto.Email,
                    GoogleId = googleLoginDto.GoogleId,
                    ProfilePictureUrl = googleLoginDto.Picture,
                    KnownAs = googleLoginDto.Name,
                    IsGoogleUser = true,
                    EmailConfirmed = true, // Google users are pre-verified
                    DateOfBirth = DateTime.Now.AddYears(-18), // Default age, user can update later
                    Gender = "Other", // Default gender, user can update later
                    Created = DateTime.Now,
                    LastActive = DateTime.Now,
                    Role = "Member"
                };

                await _userService.AddUser(newUser);

                return new UserDTO
                {
                    Username = newUser.UserName,
                    Token = _tokenService.CreateToken(newUser),
                    Role = newUser.Role
                };
            }
            catch (Exception ex)
            {
                return BadRequest($"Google login failed: {ex.Message}");
            }
        }

        [HttpGet("GoogleCallback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code)
        {
            try
            {
                // Exchange authorization code for tokens
                var tokenResponse = await ExchangeCodeForTokens(code);

                // Get user info from Google
                var userInfo = await GetGoogleUserInfo(tokenResponse.AccessToken);

                // Check if user exists with this Google ID
                var existingUser = await _userService.GetUserByGoogleId(userInfo.Subject);

                if (existingUser != null)
                {
                    // User exists, check if banned
                    var isBanned = await _adminService.IsUserBannedAsync(existingUser.Id);
                    if (isBanned)
                    {
                        return Redirect($"{Request.Scheme}://{Request.Host}/login?error=banned");
                    }

                    // Redirect to frontend with success
                    return Redirect(
                        $"{Request.Scheme}://{Request.Host}/login?google=success&username={existingUser.UserName}"
                    );
                }

                // User doesn't exist, create new user
                var newUser = new AppUser
                {
                    UserName =
                        userInfo.Email?.Split('@')[0]
                        + "_"
                        + Guid.NewGuid().ToString().Substring(0, 8),
                    Email = userInfo.Email,
                    GoogleId = userInfo.Subject,
                    ProfilePictureUrl = userInfo.Picture,
                    KnownAs = userInfo.Name,
                    IsGoogleUser = true,
                    EmailConfirmed = true,
                    DateOfBirth = DateTime.Now.AddYears(-18),
                    Gender = "Other",
                    Created = DateTime.Now,
                    LastActive = DateTime.Now,
                    Role = "Member"
                };

                await _userService.AddUser(newUser);

                // Redirect to frontend with success
                return Redirect(
                    $"{Request.Scheme}://{Request.Host}/login?google=success&username={newUser.UserName}"
                );
            }
            catch (Exception ex)
            {
                return Redirect($"{Request.Scheme}://{Request.Host}/login?error=google_failed");
            }
        }

        private async Task<GoogleTokenResponse> ExchangeCodeForTokens(string code)
        {
            using var client = new HttpClient();

            var tokenRequest = new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", "YOUR_GOOGLE_CLIENT_ID"),
                    new KeyValuePair<string, string>("client_secret", "YOUR_GOOGLE_CLIENT_SECRET"),
                    new KeyValuePair<string, string>(
                        "redirect_uri",
                        $"{Request.Scheme}://{Request.Host}/api/Account/GoogleCallback"
                    ),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                }
            );

            var response = await client.PostAsync(
                "https://oauth2.googleapis.com/token",
                tokenRequest
            );
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to exchange code for tokens: {responseContent}");
            }

            return JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
        }

        private async Task<GoogleUserInfo> GetGoogleUserInfo(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get user info: {responseContent}");
            }

            return JsonSerializer.Deserialize<GoogleUserInfo>(responseContent);
        }

        // Helper classes for Google OAuth
        public class GoogleTokenResponse
        {
            public string? AccessToken { get; set; }
            public string? TokenType { get; set; }
            public int ExpiresIn { get; set; }
            public string? RefreshToken { get; set; }
        }

        public class GoogleUserInfo
        {
            public string? Subject { get; set; }
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Picture { get; set; }
            public string? GivenName { get; set; }
            public string? FamilyName { get; set; }
        }
    }
}
