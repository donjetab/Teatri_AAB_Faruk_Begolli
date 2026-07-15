using Microsoft.AspNetCore.Mvc;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "AAB Theatre Faruk Begolli API",
            timestampUtc = DateTimeOffset.UtcNow
        });
    }
}
