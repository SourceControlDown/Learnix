using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.UnpublishCourse;

public sealed record UnpublishCourseCommand(Guid CourseId) : IRequest<Result>;
