using CleanArchitecture.Application.Features.Auth;
using CleanArchitecture.Application.Features.Auth.Models;
using CleanArchitecture.Application.Features.Users.Models;
using CleanArchitecture.WebApi.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.WebApi.Controllers;

public class AuthController(IAuthService authService, IOptions<RefreshTokenCookieSettings> cookieOptions)
    : ApiControllerBase
{
    private readonly RefreshTokenCookieSettings _cookieSettings = cookieOptions.Value;

    /// <summary>Registers a new user and signs them in.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    /// <summary>Authenticates a user. The refresh token is returned only as an HttpOnly cookie.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    /// <summary>Exchanges the refresh token cookie for a new access token (token rotation).</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_cookieSettings.Name];
        var result = await authService.RefreshTokenAsync(refreshToken ?? string.Empty, cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    /// <summary>Revokes the refresh token and clears the cookie. Idempotent.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_cookieSettings.Name];
        await authService.LogoutAsync(refreshToken, cancellationToken);
        DeleteRefreshTokenCookie();

        return NoContent();
    }

    /// <summary>Changes the password. All refresh tokens are revoked; a fresh pair is issued.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(request, cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    /// <summary>Returns the currently authenticated user.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(cancellationToken);

        return Ok(user);
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt) =>
        Response.Cookies.Append(_cookieSettings.Name, token, BuildCookieOptions(expiresAt));

    private void DeleteRefreshTokenCookie() =>
        Response.Cookies.Delete(_cookieSettings.Name, BuildCookieOptions(null));

    private CookieOptions BuildCookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = _cookieSettings.Secure,
        SameSite = Enum.TryParse<SameSiteMode>(_cookieSettings.SameSite, ignoreCase: true, out var mode)
            ? mode
            : SameSiteMode.Strict,
        Path = _cookieSettings.Path,
        Expires = expiresAt
    };
}
