using Microsoft.AspNetCore.Mvc;
using AutoHost.Models;

namespace AutoHost.Controllers;

[ApiController]
[Route("api")]
public class VersionController : ControllerBase
{
    [HttpGet("version", Name = "GetVersion")]
    [ProducesResponseType(typeof(VersionResponse), 200)]
    public ActionResult<VersionResponse> GetVersion()
    {
        return Ok(new VersionResponse
        {
            Version = "1.0.0",
            Status = "ok"
        });
    }
}