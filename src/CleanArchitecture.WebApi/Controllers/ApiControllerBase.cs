using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    public abstract class ApiControllerBase : ControllerBase
    {
    }
}
