namespace CleanArchitecture.WebApi.Helpers.Constants
{
    public class AuthCookieSettings
    {
        public const string SectionName = "AuthCookies";

        public string AccessTokenName { get; set; } = "accessToken";

        /// <summary>The access cookie must accompany every API request, so it is scoped to the whole site.</summary>
        public string AccessTokenPath { get; set; } = "/";

        public string RefreshTokenName { get; set; } = "refreshToken";

        /// <summary>Restrict the refresh cookie to auth endpoints so it is not sent with every request.</summary>
        public string RefreshTokenPath { get; set; } = "/api/v1/auth";

        /// <summary>"Strict" | "Lax" | "None". Use "None" (+ Secure) when the SPA runs on a different site.</summary>
        public string SameSite { get; set; } = "Strict";

        public bool Secure { get; set; } = true;
    }
}
