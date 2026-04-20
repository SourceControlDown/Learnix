using FluentResults;
using MediatR;

namespace Learnix.Application.Lessons.Commands.UpdateVideoLesson;

public sealed record UpdateVideoLessonCommand(
    Guid CourseId,
    Guid LessonId,
    string Title,
    string VideoUrl,
    string? Description,
    int? DurationSeconds) : IRequest<Result>;
