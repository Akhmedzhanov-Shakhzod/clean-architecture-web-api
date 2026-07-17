namespace CleanArchitecture.Application.Helpers.ApplicationExceptions
{
    public class UnauthorizedException(string message = "Unauthorized.") : AppException(message, 401);
}
