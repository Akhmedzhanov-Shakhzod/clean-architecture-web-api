using CleanArchitecture.Application.Helpers.ApplicationExceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CleanArchitecture.WebApi.Helpers.Filters
{
    /// <summary>
    /// Перехватывает ожидаемые ошибки (AppException) внутри MVC-пайплайна:
    /// клиент получает структурированный ProblemDetails, в лог пишется одна строка без стектрейса.
    /// Неожиданные исключения пролетают дальше — их ловит GlobalExceptionHandler и логирует как ошибку.
    /// </summary>
    public class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not AppException appException)
                return;

            logger.LogInformation("{ExceptionType} at {Path}: {Message}",
                appException.GetType().Name, context.HttpContext.Request.Path, appException.Message);

            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = appException.StatusCode,
                Title = TitleFor(appException.StatusCode),
                Detail = appException.Message
            })
            {
                StatusCode = appException.StatusCode
            };
            context.ExceptionHandled = true;
        }

        private static string TitleFor(int statusCode) => statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            _ => "Error"
        };
    }
}
