using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseById;

public sealed record GetCourseByIdQuery(Guid CourseId) : IRequest<Result<CourseDetailDto>>;
