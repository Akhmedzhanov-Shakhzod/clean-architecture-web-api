using CleanArchitecture.Application.Dtos.Auth;
using CleanArchitecture.Application.Dtos.Users;
using CleanArchitecture.Application.Services.Auth;
using CleanArchitecture.WebApi.Helpers.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.WebApi.Controllers.Auth
{
    [Route(AppRoutes.Auth)]
    public class AuthController(IAuthService authService, IOptions<AuthCookieSettings> cookieOptions)
        : BaseController
    {
        private readonly AuthCookieSettings _cookieSettings = cookieOptions.Value;

        /// <summary>Регистрация нового пользователя со входом.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await authService.RegisterAsync(request, cancellationToken);
            SetAuthCookies(result);

            return Ok(result.User);
        }

        /// <summary>Вход. Access- и refresh-токены возвращаются только в HttpOnly cookies.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await authService.LoginAsync(request, cancellationToken);
            SetAuthCookies(result);

            return Ok(result.User);
        }

        /// <summary>Обмен refresh-cookie на новую пару токенов (ротация).</summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> Refresh(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenName];
            var result = await authService.RefreshTokenAsync(refreshToken ?? string.Empty, cancellationToken);
            SetAuthCookies(result);

            return Ok(result.User);
        }

        /// <summary>Отзыв refresh-токена и удаление cookies. Идемпотентен.</summary>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenName];
            await authService.LogoutAsync(refreshToken, cancellationToken);
            DeleteAuthCookies();

            return NoContent();
        }

        /// <summary>Смена пароля. Все refresh-токены отзываются, выдаётся новая пара.</summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> ChangePassword(
            ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await authService.ChangePasswordAsync(request, cancellationToken);
            SetAuthCookies(result);

            return Ok(result.User);
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

        private void SetAuthCookies(AuthResult result)
        {
            Response.Cookies.Append(_cookieSettings.AccessTokenName, result.AccessToken,
                BuildCookieOptions(_cookieSettings.AccessTokenPath, result.AccessTokenExpiresAt));
            Response.Cookies.Append(_cookieSettings.RefreshTokenName, result.RefreshToken,
                BuildCookieOptions(_cookieSettings.RefreshTokenPath, result.RefreshTokenExpiresAt));
        }

        private void DeleteAuthCookies()
        {
            Response.Cookies.Delete(_cookieSettings.AccessTokenName,
                BuildCookieOptions(_cookieSettings.AccessTokenPath, null));
            Response.Cookies.Delete(_cookieSettings.RefreshTokenName,
                BuildCookieOptions(_cookieSettings.RefreshTokenPath, null));
        }

        private CookieOptions BuildCookieOptions(string path, DateTime? expiresAt) => new()
        {
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = Enum.TryParse<SameSiteMode>(_cookieSettings.SameSite, ignoreCase: true, out var mode)
                ? mode
                : SameSiteMode.Strict,
            Path = path,
            Expires = expiresAt
        };
    }
}
