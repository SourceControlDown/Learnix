using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPublicCourses;

public sealed record GetPublicCoursesQuery(
    string? Search,
    int Skip,
    int Take,
    Guid? CategoryId) : IRequest<Result<PaginatedResult<PublicCourseCardDto>>>;
