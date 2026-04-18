using Learnix.Application.Auth.Commands.ConfirmEmail;
using Learnix.Application.Auth.Commands.Register;
using Learnix.Application.Auth.Commands.ResendConfirmationEmail;
using Learnix.Application.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.HasError<ValidationError>(out var validationErrors))
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));
        }
        if (result.HasError<ConflictError>())
        {
            return Conflict(new ProblemDetails 
            { 
                Title = "Conflict", 
                Detail = result.Errors[0].Message, 
                Status = 409 
            });
        }
        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Registration failed", 
                Detail = string.Join("; ", result.Errors.Select(e => e.Message)), 
                Status = 400 
            });
        }

        return CreatedAtAction(nameof(Register), result.Value);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.HasError<ValidationError>(out var validationErrors))
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));
        if (result.HasError<NotFoundError>())
            return NotFound();
        if (result.IsFailed)
            return BadRequest(new ProblemDetails { Title = "Email confirmation failed", Detail = string.Join("; ", result.Errors.Select(e => e.Message)), Status = 400 });

        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.HasError<ValidationError>(out var validationErrors))
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));

        // Always 204 — anti-enumeration
        return NoContent();
    }
}