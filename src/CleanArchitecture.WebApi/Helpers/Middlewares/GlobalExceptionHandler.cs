using CleanArchitecture.Application.Helpers.ApplicationExceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Helpers.Middlewares
{
    /// <summary>
    /// Последний рубеж: превращает исключения в ProblemDetails (RFC 7807).
    /// Ожидаемые ошибки обычно перехватывает ApiExceptionFilter раньше;
    /// сюда долетают неожиданные исключения и ошибки вне MVC-пайплайна.
    /// </summary>
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problemDetails = exception switch
            {
                AppException appException => new ProblemDetails
                {
                    Status = appException.StatusCode,
                    Title = appException.Message
                },

                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred."
                }
            };

            if (problemDetails.Status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled exception at {Path}", httpContext.Request.Path);
            else
                logger.LogInformation("Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

            httpContext.Response.StatusCode = problemDetails.Status!.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
