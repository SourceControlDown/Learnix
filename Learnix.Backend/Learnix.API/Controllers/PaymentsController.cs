using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.Payments.Commands.InitiateMockPayment;
using Learnix.Application.Payments.Queries.GetMyPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Payments)]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiateMockPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyPaymentsQuery(skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}
