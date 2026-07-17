namespace CleanArchitecture.Application.Helpers.ApplicationExceptions
{
    public class ForbiddenException(string message = "Forbidden.") : AppException(message, 403);
}
