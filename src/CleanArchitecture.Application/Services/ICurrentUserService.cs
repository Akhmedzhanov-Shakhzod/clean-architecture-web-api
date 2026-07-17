namespace CleanArchitecture.Application.Services
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? IpAddress { get; }
        bool IsAuthenticated { get; }
    }
}
