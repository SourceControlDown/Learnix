using Learnix.Application.Auth.Commands.ConfirmEmail;
using Learnix.Application.Auth.Commands.Login;
using Learnix.Application.Auth.Commands.Logout;
using Learnix.Application.Auth.Commands.RefreshToken;
using Learnix.Application.Auth.Commands.Register;
using Learnix.Application.Auth.Commands.ResendConfirmationEmail;
using Learnix.Application.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    private const string RefreshCookieName = "learnix_refresh";
    private const string RefreshCookiePath = "/api/auth";

    // Registration
    // =================================

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
                Status = StatusCodes.Status409Conflict
            });
        }
        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Registration failed", 
                Detail = string.Join("; ", result.Errors.Select(e => e.Message)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        return CreatedAtAction(nameof(Register), result.Value);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.HasError<ValidationError>(out var validationErrors))
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));
        }

        if (result.HasError<NotFoundError>())
        {
            return NotFound();
        }

        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Email confirmation failed", 
                Detail = string.Join("; ", result.Errors.Select(e => e.Message)), 
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.HasError<ValidationError>(out var validationErrors))
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));
        }

        // Always 204 — anti-enumeration
        return NoContent();
    }

    // Login / Refresh / Logout
    // =================================

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.HasError<ValidationError>(out var validationErrors))
        {
            return BadRequest(new ValidationProblemDetails(validationErrors.First().ToDictionary()));
        }

        if (result.HasError<ForbiddenError>())
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = string.Join("; ", result.Errors.Select(e => e.Message))
            });
        }

        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Login failed" 
            });
        }

        var response = result.Value;

        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

        return Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails 
            { 
                Title = "No refresh token." 
            });
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), ct);

        if (result.HasError<ForbiddenError>())
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new ProblemDetails
            {
                Title = "Refresh failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = string.Join("; ", result.Errors.Select(e => e.Message))
            });
        }

        if (result.IsFailed)
        {
            return BadRequest(new ProblemDetails 
            { 
                Title = "Refresh failed" 
            });
        }

        var response = result.Value;
        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

        return Ok(new
        {
            response.AccessToken,
            response.AccessTokenExpiresAt
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        await sender.Send(new LogoutCommand(refreshToken ?? string.Empty), ct);
        ClearRefreshTokenCookie();
        return NoContent();
    }

    // Cookie helpers
    // =================================
    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,       // localhost over HTTPS works; plain HTTP localhost is also accepted by browsers
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expiresAt
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = RefreshCookiePath
        });
    }
}