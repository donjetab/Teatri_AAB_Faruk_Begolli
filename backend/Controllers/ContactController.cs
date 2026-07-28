using Microsoft.AspNetCore.Mvc;
using Theatre.Api.DTOs;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/contact")]
[Produces("application/json")]
public sealed class ContactController(IContactService contactService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ContactMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactMessageResponse>> Send(
        [FromBody] ContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await contactService.SendAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
