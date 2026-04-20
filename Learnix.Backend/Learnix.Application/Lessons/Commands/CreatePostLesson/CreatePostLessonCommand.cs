using FluentResults;
using MediatR;

namespace Learnix.Application.Lessons.Commands.CreatePostLesson;

public sealed record CreatePostLessonCommand(
    Guid CourseId,
    Guid SectionId,
    string Title,
    string Content) : IRequest<Result<Guid>>;
