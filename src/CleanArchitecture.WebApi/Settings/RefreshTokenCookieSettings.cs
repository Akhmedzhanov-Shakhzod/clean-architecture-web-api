namespace CleanArchitecture.WebApi.Settings;

public class RefreshTokenCookieSettings
{
    public const string SectionName = "RefreshTokenCookie";

    public string Name { get; set; } = "refreshToken";

    /// <summary>Restrict the cookie to auth endpoints so it is not sent with every request.</summary>
    public string Path { get; set; } = "/api/auth";

    /// <summary>"Strict" | "Lax" | "None". Use "None" (+ Secure) when the SPA runs on a different site.</summary>
    public string SameSite { get; set; } = "Strict";

    public bool Secure { get; set; } = true;
}
