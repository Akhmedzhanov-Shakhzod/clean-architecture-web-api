namespace CleanArchitecture.Application.Helpers.ApplicationExceptions
{
    public class NotFoundException(string message = "Resource not found.") : AppException(message, 404);
}
