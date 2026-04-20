using Learnix.API.Extensions;
using Learnix.Application.Courses.Commands.ArchiveCourse;
using Learnix.Application.Courses.Commands.CreateCourse;
using Learnix.Application.Courses.Commands.DeleteCourse;
using Learnix.Application.Courses.Commands.PublishCourse;
using Learnix.Application.Courses.Commands.UnpublishCourse;
using Learnix.Application.Courses.Commands.UpdateCourseDetails;
using Learnix.Application.Courses.Queries.GetCourseById;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value =>
            CreatedAtAction(nameof(GetById), new { id = value.CourseId }, value));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCourseRequest body,
        CancellationToken ct)
    {
        var command = new UpdateCourseDetailsCommand(
            id,
            body.CategoryId,
            body.Title,
            body.Description,
            body.Price,
            body.CoverImageUrl,
            body.Tags);

        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new PublishCourseCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new UnpublishCourseCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ArchiveCourseCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCourseCommand(id), ct);
        return result.ToActionResult();
    }
}

/// <summary>
/// Request body for PUT /api/courses/{id}. Separate from UpdateCourseDetailsCommand because
/// CourseId comes from the route, not the body — this prevents a conflicting id in the payload.
/// </summary>
public sealed record UpdateCourseRequest(
    [property: JsonRequired] Guid CategoryId,
    string Title,
    string Description,
    [property: JsonRequired] decimal Price,
    string? CoverImageUrl,
    IEnumerable<string> Tags);
