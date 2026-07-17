using CleanArchitecture.Application.Helpers.Paginations;
using CleanArchitecture.Application.Dtos.Users;

namespace CleanArchitecture.Application.Services.Users
{
    public interface IUserService
    {
        Task<PagedList<UserDto>> GetUsersAsync(PagedRequest request, CancellationToken cancellationToken = default);
        Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    }
}
