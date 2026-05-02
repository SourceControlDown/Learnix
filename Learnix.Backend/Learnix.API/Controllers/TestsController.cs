using Learnix.API.Extensions;
using Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;
using Learnix.Application.TestAttempts.Queries.GetMyTestAttempts;
using Learnix.Application.TestAttempts.Queries.GetTestLesson;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/lessons/{lessonId:guid}/test")]
[Authorize]
public sealed class TestsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTestLesson(
        Guid courseId, Guid lessonId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTestLessonQuery(courseId, lessonId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("attempts")]
    public async Task<IActionResult> SubmitAttempt(
        Guid courseId, Guid lessonId,
        [FromBody] SubmitAttemptRequest request,
        CancellationToken ct)
    {
        var command = new SubmitTestAttemptCommand(courseId, lessonId, request.Answers);
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("attempts")]
    public async Task<IActionResult> GetMyAttempts(
        Guid courseId, Guid lessonId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyTestAttemptsQuery(courseId, lessonId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

public sealed record SubmitAttemptRequest(IReadOnlyList<SubmittedAnswerDto> Answers);
