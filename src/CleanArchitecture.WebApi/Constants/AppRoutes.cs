namespace CleanArchitecture.WebApi.Constants
{
    public static class AppRoutes
    {
        public const string Api = "api/";
        public const string Version = "v{version:apiVersion}/";

        public const string Base = Api + Version;

        public const string Auth = Base + "auth";
        public const string Users = Base + "users";
    }
}
