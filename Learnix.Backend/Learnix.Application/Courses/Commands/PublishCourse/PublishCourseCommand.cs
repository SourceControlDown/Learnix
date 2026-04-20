using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.PublishCourse;

public sealed record PublishCourseCommand(Guid CourseId) : IRequest<Result>;
