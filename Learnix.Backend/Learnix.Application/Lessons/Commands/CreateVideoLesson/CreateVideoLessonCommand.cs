using FluentResults;
using MediatR;

namespace Learnix.Application.Lessons.Commands.CreateVideoLesson;

public sealed record CreateVideoLessonCommand(
    Guid CourseId,
    Guid SectionId,
    string Title,
    string VideoUrl,
    string? Description,
    int? DurationSeconds) : IRequest<Result<Guid>>;
