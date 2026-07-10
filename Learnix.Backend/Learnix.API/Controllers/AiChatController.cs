using System.Text;
using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Commands.ClearChatSession;
using Learnix.Application.AiChat.Queries.GetChatSession;
using Learnix.Application.AiChat.Services;
using Learnix.Application.Common.Abstractions.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

/// <summary>
/// A chat session is identified by the signed-in user and the scope in the route (ADR-CHAT-004).
/// The platform assistant and a course tutor are separate conversations.
/// </summary>
[ApiController]
[Route("api/ai-chat")]
[Authorize]
public sealed class AiChatController(
    ISender sender,
    ChatStreamOrchestrator orchestrator,
    ChatScopeAuthorizer authorizer,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("platform/session")]
    public Task<IActionResult> GetPlatformSession(CancellationToken ct) =>
        GetSession(ChatScope.Platform, ct);

    [HttpGet("courses/{courseId:guid}/session")]
    public Task<IActionResult> GetCourseSession(Guid courseId, CancellationToken ct) =>
        GetSession(ChatScope.ForCourse(courseId), ct);

    [HttpDelete("platform/session")]
    public Task<IActionResult> ClearPlatformSession(CancellationToken ct) =>
        ClearSession(ChatScope.Platform, ct);

    [HttpDelete("courses/{courseId:guid}/session")]
    public Task<IActionResult> ClearCourseSession(Guid courseId, CancellationToken ct) =>
        ClearSession(ChatScope.ForCourse(courseId), ct);

    [HttpPost("platform/messages")]
    [EnableRateLimiting(RateLimitPolicies.AiChatPlatform)]
    public Task StreamPlatformMessage([FromBody] SendMessageRequest request, CancellationToken ct) =>
        StreamMessage(ChatScope.Platform, request.Message, lessonId: null, ct);

    [HttpPost("courses/{courseId:guid}/messages")]
    [EnableRateLimiting(RateLimitPolicies.AiChatTutor)]
    public Task StreamCourseMessage(
        Guid courseId,
        [FromBody] SendCourseMessageRequest request,
        CancellationToken ct) =>
        StreamMessage(ChatScope.ForCourse(courseId), request.Message, request.LessonId, ct);

    private async Task<IActionResult> GetSession(ChatScope scope, CancellationToken ct)
    {
        var result = await sender.Send(new GetChatSessionQuery(scope), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    private async Task<IActionResult> ClearSession(ChatScope scope, CancellationToken ct)
    {
        var result = await sender.Send(new ClearChatSessionCommand(scope), ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// The stream bypasses the MediatR pipeline, so validation and the scope check run here, before any
    /// SSE header is written — a rejected request must still be able to answer with a status code.
    /// </summary>
    private async Task StreamMessage(ChatScope scope, string message, Guid? lessonId, CancellationToken ct)
    {
        if (currentUser.UserId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userId = currentUser.UserId.Value;

        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Message cannot be empty" }, ct);
            return;
        }

        if (message.Length > AiChatConstants.MessageMaxLength)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                new { error = $"Message cannot exceed {AiChatConstants.MessageMaxLength} characters." },
                ct);
            return;
        }

        var access = await authorizer.EnsureAccessAsync(userId, scope, ct);
        if (access.IsFailed)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await Response.WriteAsJsonAsync(new { error = access.Errors[0].Message }, ct);
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var sseEvent in orchestrator.StreamAsync(userId, scope, lessonId, message, ct))
        {
            var line = $"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(line), ct);
            await Response.Body.FlushAsync(ct);

            if (sseEvent.EventType == "message_end")
                break;
        }
    }
}

public sealed record SendMessageRequest(string Message);

/// <param name="LessonId">The lesson the student has open, if any. Not a claim about access — it is re-checked.</param>
public sealed record SendCourseMessageRequest(string Message, Guid? LessonId);
