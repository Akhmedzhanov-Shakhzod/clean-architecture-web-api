using CleanArchitecture.Application.Services.Auth;
using CleanArchitecture.Application.Dtos.Auth;
using CleanArchitecture.Application.Dtos.Users;
using CleanArchitecture.WebApi.Helpers.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.WebApi.Controllers
{
    [Route(AppRoutes.Auth)]
    public class AuthController(IAuthService authService, IOptions<RefreshTokenCookieSettings> cookieOptions)
        : BaseController
    {
        private readonly RefreshTokenCookieSettings _cookieSettings = cookieOptions.Value;

        /// <summary>Регистрация нового пользователя со входом.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await authService.RegisterAsync(request, cancellationToken);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

            return Ok(result.Response);
        }

        /// <summary>Вход. Refresh-токен возвращается только в HttpOnly cookie.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await authService.LoginAsync(request, cancellationToken);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

            return Ok(result.Response);
        }

        /// <summary>Обмен refresh-cookie на новый access token (ротация).</summary>
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

        /// <summary>Отзыв refresh-токена и удаление cookie. Идемпотентен.</summary>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[_cookieSettings.Name];
            await authService.LogoutAsync(refreshToken, cancellationToken);
            DeleteRefreshTokenCookie();

            return NoContent();
        }

        /// <summary>Смена пароля. Все refresh-токены отзываются, выдаётся новая пара.</summary>
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

        /// <summary>Текущий пользователь.</summary>
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
}
