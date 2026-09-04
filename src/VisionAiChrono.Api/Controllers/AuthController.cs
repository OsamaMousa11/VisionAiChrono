using System.Security.Claims;
using Karaakeb.Core.DTO.AuthenticationDTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CleanArchitectureTemplate_Application.Dtos;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Application.ServiceContract;
using Swashbuckle.AspNetCore.Annotations;

namespace VisionAiChrono.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationServices _authService;

    public AuthController(IAuthenticationServices authService)
    {
        _authService = authService;
    }

    [HttpGet("google")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Start Google OAuth login",
        Description = "Redirects the browser to Google for authentication.")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Google OAuth callback",
        Description = "Handles Google redirect, creates/links the user, and issues a JWT.")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
            throw new BadRequestException("Google authentication failed.");

        var principal = authenticateResult.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        var name = principal.FindFirstValue(ClaimTypes.Name)
                   ?? principal.FindFirstValue("name");
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? throw new BadRequestException("Google did not return a user id.");

        var result = await _authService.ExternalLoginAsync(
            GoogleDefaults.AuthenticationScheme,
            providerKey,
            email ?? string.Empty,
            name);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.IsAuthenticated)
            throw new BadRequestException(result.Message);

        SetRefreshToken(result.RefreshToken, result.RefreshTokenExpiration);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            var separator = returnUrl.Contains('?') ? "&" : "?";
            return Redirect($"{returnUrl}{separator}token={Uri.EscapeDataString(result.Token)}");
        }

        return Ok(new ApiResponse<LoginResponseDTO>(new LoginResponseDTO
        {
            Token = result.Token,
            Email = result.Email,
            Username = result.Username,
            Roles = result.Roles,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiration = result.RefreshTokenExpiration,
        }, "Google login successful."));
    }

    private void SetRefreshToken(string refreshToken, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = expires,
            SameSite = SameSiteMode.Lax
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
