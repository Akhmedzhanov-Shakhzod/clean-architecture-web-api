using CleanArchitecture.Application.Features.Users.Models;

namespace CleanArchitecture.Application.Features.Auth.Models;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName);

public record LoginRequest(string Email, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>What the client receives in the response body. The refresh token never appears here — it travels only in an HttpOnly cookie.</summary>
public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime AccessTokenExpiresAt { get; init; }
    public UserDto User { get; init; } = default!;
}

/// <summary>Internal result: the API layer puts <see cref="RefreshToken"/> into the cookie and returns <see cref="Response"/> as the body.</summary>
public record AuthResult
{
    public AuthResponse Response { get; init; } = default!;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; init; }
}
