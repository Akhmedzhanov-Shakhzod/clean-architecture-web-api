namespace CleanArchitecture.Application.Helpers.ApplicationExceptions
{
    /// <summary>Базовый класс ожидаемых ошибок приложения; API-слой мапит их на HTTP-статусы.</summary>
    public abstract class AppException(string message, int statusCode) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }
}
