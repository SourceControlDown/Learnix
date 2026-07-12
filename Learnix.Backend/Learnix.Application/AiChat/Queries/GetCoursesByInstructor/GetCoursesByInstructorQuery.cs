using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetCoursesByInstructor;

/// <summary>
/// Looks up an instructor by display name or by id, and returns their published courses.
/// The AI normally only knows the name, so <paramref name="InstructorName"/> is the common entry point.
/// </summary>
public sealed record GetCoursesByInstructorQuery(
    string? InstructorName = null,
    Guid? InstructorId = null) : IRequest<Result<InstructorCoursesDto>>;
