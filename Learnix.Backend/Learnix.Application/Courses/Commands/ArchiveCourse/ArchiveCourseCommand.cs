using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Commands.ArchiveCourse;

public sealed record ArchiveCourseCommand(Guid CourseId) : IRequest<Result>;
