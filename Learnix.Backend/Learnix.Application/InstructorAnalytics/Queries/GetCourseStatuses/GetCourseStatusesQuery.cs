using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;

public sealed record GetCourseStatusesQuery : IRequest<Result<CourseStatusesDto>>;

public sealed record CourseStatusesDto(
    int Draft,
    int Published,
    int Archived);
