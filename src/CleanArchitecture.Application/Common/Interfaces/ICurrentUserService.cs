namespace CleanArchitecture.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? IpAddress { get; }
        bool IsAuthenticated { get; }
    }
}
