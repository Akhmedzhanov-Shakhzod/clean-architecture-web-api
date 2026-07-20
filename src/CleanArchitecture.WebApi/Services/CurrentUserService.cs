using System.Security.Claims;
using CleanArchitecture.Application.Services;

namespace CleanArchitecture.WebApi.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        private HttpContext? HttpContext => httpContextAccessor.HttpContext;

        public Guid? UserId
        {
            get
            {
                var value = HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public string? IpAddress =>
            HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? HttpContext?.Connection.RemoteIpAddress?.ToString();

        public bool IsAuthenticated => HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
