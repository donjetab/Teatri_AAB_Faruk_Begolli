using Microsoft.AspNetCore.Mvc;
using Theatre.Api.DTOs;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/newsletter")]
[Produces("application/json")]
public sealed class NewsletterController(INewsletterService newsletterService) : ControllerBase
{
    /// <summary>
    /// Subscribes an email address to the public newsletter.
    /// </summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(NewsletterSubscribeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NewsletterSubscribeResponse>> Subscribe(
        [FromBody] NewsletterSubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await newsletterService.SubscribeAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
