using Microsoft.AspNetCore.Mvc;
using Theatre.Api.DTOs;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/{languageCode:regex(^(sq|en)$)}/home")]
[Produces("application/json")]
public sealed class HomeController(IHomepageService homepageService) : ControllerBase
{
    /// <summary>
    /// Returns the public homepage content for the requested language.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HomeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeResponseDto>> Get(string languageCode, CancellationToken cancellationToken)
    {
        var home = await homepageService.GetHomeAsync(languageCode, cancellationToken);
        return home is null ? NotFound() : Ok(home);
    }
}
