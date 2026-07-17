using CleanArchitecture.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CleanArchitecture.WebApi.Filters;

/// <summary>
/// Перехватывает ожидаемые ошибки (валидация, AppException) внутри MVC-пайплайна:
/// клиент получает структурированный ProblemDetails, в лог пишется одна строка без стектрейса.
/// Неожиданные исключения пролетают дальше — их ловит GlobalExceptionHandler и логирует как ошибку.
/// </summary>
public class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path;

        switch (context.Exception)
        {
            case ValidationException validationException:
            {
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                logger.LogInformation("Validation failed at {Path}: {Errors}",
                    path,
                    string.Join("; ", validationException.Errors
                        .Select(e => $"{e.PropertyName} — {e.ErrorMessage}")));

                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Detail = "One or more fields are invalid. See 'errors' for details."
                });
                context.ExceptionHandled = true;
                break;
            }

            case AppException appException:
            {
                logger.LogInformation("{ExceptionType} at {Path}: {Message}",
                    appException.GetType().Name, path, appException.Message);

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
                break;
            }
        }
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
