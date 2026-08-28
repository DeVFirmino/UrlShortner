using Microsoft.AspNetCore.Mvc;
using UrlShortner.Contracts;

namespace UrlShortner.Controllers;

[ApiController]
public sealed class ServerController : ControllerBase
{
    /// <summary>
    /// Names the replica that answered, which is how the load balancer's
    /// round-robin is demonstrated.
    /// </summary>
    [HttpGet("/whoami")]
    [ProducesResponseType(typeof(ServerIdentityResponse), StatusCodes.Status200OK)]
    public IActionResult WhoAmI()
    {
        return Ok(new ServerIdentityResponse { Server = Environment.MachineName });
    }
}
