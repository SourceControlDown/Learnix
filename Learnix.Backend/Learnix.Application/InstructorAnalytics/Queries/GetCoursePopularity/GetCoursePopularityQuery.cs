using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetCoursePopularity;

public sealed record GetCoursePopularityQuery : IRequest<Result<List<CoursePopularityItemDto>>>;

public sealed record CoursePopularityItemDto(
    Guid CourseId,
    string Title,
    int Enrollments);
