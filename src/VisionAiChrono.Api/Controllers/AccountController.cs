using Karaakeb.Core.DTO.AuthenticationDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CleanArchitectureTemplate_Application.Dtos;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Application.ServiceContract;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;

namespace CleanArchitectureTemplate_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAuthenticationServices _authService;
        private readonly ITokenBlacklistService _blacklistService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthenticationServices authService,
            ITokenBlacklistService blacklistService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _blacklistService = blacklistService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Register a new user",
            Description = "Creates a new USER account and sends OTP to email for verification")]
        [ProducesResponseType(typeof(ApiResponse<RegisterResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            await _authService.RegisterAsync(dto);
            await _authService.ResendOtpAsync(new ResendOtpDTO { Email = dto.Email });
            return Created(string.Empty, new ApiResponse<RegisterResponseDTO>
            {
                Success = true,
                Message = "Registration completed successfully.",
                Data = new RegisterResponseDTO
                {
                    Message = "Verification code has been sent to your email.",
                    Email = dto.Email,
                    Username = dto.UserName
                }
            });
        }

        [HttpPost("resend-otp")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Resend OTP",
            Description = "Resends the OTP verification code to the user's email")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDTO dto)
        {
            await _authService.ResendOtpAsync(dto);
            return Ok(new ApiResponse<string> { Success = true, Message = "Verification code has been resent successfully.", Data = string.Empty });
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Verify OTP code", Description = "Verifies the 6-digit code sent to user email. OTP expires after 5 minutes.")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO dto)
        {
            var authResponse = await _authService.VerifyOtpAsync(dto);
            SetRefreshToken(authResponse.RefreshToken, authResponse.RefreshTokenExpiration);

            return Ok(new ApiResponse<LoginResponseDTO>(new LoginResponseDTO
            {
                Token = authResponse.Token,
                RefreshToken = authResponse.RefreshToken,
                RefreshTokenExpiration = authResponse.RefreshTokenExpiration,
                Email = authResponse.Email,
                Username = authResponse.Username,
                Roles = authResponse.Roles,
            }, "Verification successful."));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "User login", Description = "Authenticates user and returns JWT token + Refresh token.")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.IsAuthenticated)
                throw new BadRequestException(result.Message);

            SetRefreshToken(result.RefreshToken, result.RefreshTokenExpiration);

            return Ok(new ApiResponse<LoginResponseDTO>(new LoginResponseDTO
            {
                Token = result.Token,
                Email = result.Email,
                Username = result.Username,
                Roles = result.Roles,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiration = result.RefreshTokenExpiration,
            }, "Login successful."));
        }

        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "User logout", Description = "Blacklists the current access token and invalidates the refresh token")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var rawToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader[7..] : null;

            if (!string.IsNullOrEmpty(rawToken))
            {
                try
                {
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
                    _blacklistService.BlacklistToken(rawToken, jwt.ValidTo);
                }
                catch { }
            }

            var userId = User.FindFirst("uid")?.Value;
            if (userId != null)
                await _authService.LogoutAsync(userId);

            Response.Cookies.Delete("refreshToken");

            return Ok(new ApiResponse<string> { Success = true, Message = "Logged out successfully.", Data = string.Empty });
        }

        [HttpGet("refresh-token")]
        [SwaggerOperation(Summary = "Refresh JWT token",
            Description = "Generates a new JWT access token using the refresh token stored in the Secure Cookie. Also rotates the refresh token.")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            var token = Request.Cookies["refreshToken"]
                ?? throw new BadRequestException("Refresh token not found.");

            var result = await _authService.RefreshTokenAsync(token);

            if (!result.IsAuthenticated)
                throw new BadRequestException(result.Message);

            SetRefreshToken(result.RefreshToken, result.RefreshTokenExpiration);

            return Ok(new ApiResponse<LoginResponseDTO>(new LoginResponseDTO
            {
                Token = result.Token,
                Email = result.Email,
                Username = result.Username,
                Roles = result.Roles,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiration = result.RefreshTokenExpiration,
            }, "Token refreshed successfully."));
        }

        [HttpPost("revoke-token")]
        [SwaggerOperation(Summary = "Revoke refresh token",
            Description = "Invalidates the refresh token from body or cookie")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RevokeToken([FromBody] RevokTokenDTO dto)
        {
            var token = dto.Token ?? Request.Cookies["refreshToken"]
                ?? throw new BadRequestException("Token is required.");

            await _authService.RevokeTokenAsync(token);
            return Ok(new ApiResponse<string> { Success = true, Message = "Token revoked successfully.", Data = string.Empty });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Forgot password",
            Description = "Sends OTP to email. Also used by Worker to reset temp password")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new ApiResponse<string> { Success = true, Message = "Verification code has been sent to your email.", Data = string.Empty });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Reset password using OTP",
            Description = "Validates the OTP code sent via ForgotPassword and resets the user's password. Automatically authenticates the user upon success.")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var authResponse = await _authService.ResetPasswordAsync(dto);
            SetRefreshToken(authResponse.RefreshToken, authResponse.RefreshTokenExpiration);

            return Ok(new ApiResponse<LoginResponseDTO>(new LoginResponseDTO
            {
                Token = authResponse.Token,
                RefreshToken = authResponse.RefreshToken,
                RefreshTokenExpiration = authResponse.RefreshTokenExpiration,
                Email = authResponse.Email,
                Username = authResponse.Username,
                Roles = authResponse.Roles,
            }, "Password reset successfully."));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("admin/add-role")]
        [SwaggerOperation(Summary = "Add role to user [ADMIN]",
            Description = "Assigns a role to a specific user")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddRole([FromBody] AddRoleDTO dto)
        {
            await _authService.AddRoleToUserAsync(dto);
            return Ok(new ApiResponse<string> { Success = true, Message = "Role added successfully.", Data = string.Empty });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin/roles")]
        [SwaggerOperation(Summary = "Get all roles [ADMIN]",
            Description = "Returns all roles in the system")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CleanArchitectureTemplate_Domain.Model.Identity.ApplicationRole>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _authService.GetAllRolesAsync();
            return Ok(new ApiResponse<IEnumerable<CleanArchitectureTemplate_Domain.Model.Identity.ApplicationRole>>(roles, "Roles retrieved successfully."));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("admin/roles/{roleName}")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            await _authService.DeleteRoleAsync(roleName);
            return Ok(new ApiResponse<string> { Success = true, Message = "Role deleted successfully.", Data = string.Empty });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin/users")]
        [SwaggerOperation(Summary = "Delete role [ADMIN]",
            Description = "Deletes a role by name")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers()
        {
            var userDTOs = await _authService.GetAllUsersWithRoleUserAsync();
            return Ok(new ApiResponse<IEnumerable<UserResponseDTO>>(userDTOs, "Users retrieved successfully."));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("users/{id}")]
        [SwaggerOperation(Summary = "Get all users [ADMIN]",
            Description = "Returns all users with USER role only")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUserById(string id)
        {
            var userDTO = await _authService.GetUserByIdWithRoleUserAsync(id);
            return Ok(new ApiResponse<UserResponseDTO>(userDTO, "User retrieved successfully."));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("users/{id}")]
        [SwaggerOperation(Summary = "Get user by ID [ADMIN]",
            Description = "Returns a specific user's details. Only returns USER role")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDTO dto)
        {
            await _authService.UpdateUserAsync(id, dto);
            return Ok(new ApiResponse<string> { Success = true, Message = "User updated successfully.", Data = string.Empty });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("users/{id}")]
        [SwaggerOperation(Summary = "Update user [ADMIN]",
            Description = "Updates a user's information by ID")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _authService.DeleteUserAsync(id);
            return Ok(new ApiResponse<string> { Success = true, Message = "User deleted successfully.", Data = string.Empty });
        }

        [HttpPost("change-password")]
        [Authorize]
        [SwaggerOperation(Summary = "Change password",
            Description = "Updates the authenticated user's password. This is mandatory for users (like workers) with a temporary password flag.")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userId = User.FindFirst("uid")?.Value
                ?? throw new UnauthorizedException("Invalid token.");

            await _authService.ChangePasswordAsync(userId, dto);
            return Ok(new ApiResponse<string> { Success = true, Message = "Password changed successfully.", Data = string.Empty });
        }

        [HttpPost("send-delete-otp")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Send OTP to delete account", Description = "Sends an OTP to the provided email to confirm account deletion")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendDeleteOtp([FromBody] DeleteAccountOtpRequestDTO dto)
        {
            await _authService.ForgotPasswordAsync(new ForgotPasswordDTO { Email = dto.Email });
            return Ok(new ApiResponse<string> { Success = true, Message = "Confirmation code has been sent to your email.", Data = string.Empty });
        }

        [HttpDelete("delete-account")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Delete account by email and OTP", Description = "Deletes an account using the provided email and the OTP code sent to it")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDTO dto)
        {
            await _authService.DeleteAccountByOtpAsync(dto);
            return Ok(new ApiResponse<string> { Success = true, Message = "Account deleted successfully.", Data = string.Empty });
        }

        [HttpDelete("me")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete current user account",
    Description = "Deletes the account of the currently authenticated user")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = User.FindFirst("uid")?.Value
                ?? throw new UnauthorizedException("Invalid token.");

            await _authService.DeleteAccountByIdAsync(userId);
            return Ok(new ApiResponse<string> { Success = true, Message = "Your account has been deleted successfully.", Data = string.Empty });
        }

        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(Summary = "Get current user info",
    Description = "Reads JWT token from Authentication headers and returns full user data")]
        [ProducesResponseType(typeof(ApiResponse<MeResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedException("You must be logged in first.");

            var data = await _authService.GetMeByIdAsync(userId);
            return Ok(new ApiResponse<MeResponseDTO>(data, "User info retrieved successfully."));
        }

        [HttpPost("me-by-token")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get user by token",
    Description = "Reads JWT token from the request body and returns full authenticated user data. Throws if invalid.")]
        [ProducesResponseType(typeof(ApiResponse<MeResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMeByToken([FromBody] GetUserByTokenDTO dto)
        {
            var result = await _authService.GetMeAsync(dto.Token);
            return Ok(new ApiResponse<MeResponseDTO>(result, "User info retrieved successfully."));
        }

        private void SetRefreshToken(string refreshToken, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = expires,
                SameSite = SameSiteMode.None
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
