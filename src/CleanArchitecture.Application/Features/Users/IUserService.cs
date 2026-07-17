using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Features.Users.Models;

namespace CleanArchitecture.Application.Features.Users
{
    public interface IUserService
    {
        Task<PagedList<UserDto>> GetUsersAsync(PagedRequest request, CancellationToken cancellationToken = default);
        Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    }
}
