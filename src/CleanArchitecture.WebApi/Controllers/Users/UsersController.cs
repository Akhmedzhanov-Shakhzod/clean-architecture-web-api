using CleanArchitecture.Application.Helpers.Paginations;
using CleanArchitecture.Application.Services.Users;
using CleanArchitecture.Application.Dtos.Users;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.WebApi.Helpers.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers
{
    /// <summary>Пример защищённого контроллера с ограничением по роли.</summary>
    [Authorize(Roles = Roles.Admin)]
    [Route(AppRoutes.Users)]
    public class UsersController(IUserService userService) : BaseController
    {
        [HttpGet]
        [ProducesResponseType(typeof(PagedList<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedList<UserDto>>> GetUsers(
            [FromQuery] PagedRequest request, CancellationToken cancellationToken)
        {
            return Ok(await userService.GetUsersAsync(request, cancellationToken));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await userService.GetByIdAsync(id, cancellationToken));
        }

        [HttpPut("{id:guid}/active")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool value, CancellationToken cancellationToken)
        {
            await userService.SetActiveAsync(id, value, cancellationToken);

            return NoContent();
        }
    }
}
