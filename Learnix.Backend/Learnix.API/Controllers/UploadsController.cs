using Learnix.API.Extensions;
using Learnix.Application.Uploads.Commands.RequestUploadUrl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
public sealed class UploadsController(ISender mediator) : ControllerBase
{
    [HttpPost("request-url")]
    public async Task<IActionResult> RequestUploadUrl(
        [FromBody] RequestUploadUrlCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return result.ToActionResult();
    }
}
