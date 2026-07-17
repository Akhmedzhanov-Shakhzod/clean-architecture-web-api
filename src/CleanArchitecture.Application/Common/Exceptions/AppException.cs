namespace CleanArchitecture.Application.Common.Exceptions
{
    /// <summary>Base class for all expected application errors, mapped to HTTP status codes by the API layer.</summary>
    public abstract class AppException(string message, int statusCode) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }

    public class BadRequestException(string message) : AppException(message, 400);

    public class UnauthorizedException(string message = "Unauthorized.") : AppException(message, 401);

    public class ForbiddenException(string message = "Forbidden.") : AppException(message, 403);

    public class NotFoundException(string message = "Resource not found.") : AppException(message, 404);

    public class ConflictException(string message) : AppException(message, 409);
}
