namespace CleanArchitecture.Application.Helpers.ApplicationExceptions
{
    public class ConflictException(string message) : AppException(message, 409);
}
