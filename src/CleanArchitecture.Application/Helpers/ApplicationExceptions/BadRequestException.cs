namespace CleanArchitecture.Application.Helpers.ApplicationExceptions
{
    public class BadRequestException(string message) : AppException(message, 400);
}
