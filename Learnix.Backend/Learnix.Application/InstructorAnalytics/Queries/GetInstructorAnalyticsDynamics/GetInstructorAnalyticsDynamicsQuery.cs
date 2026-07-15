using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsDynamics;

public sealed record GetInstructorAnalyticsDynamicsQuery(DateTime StartDate, DateTime EndDate)
    : IRequest<Result<List<InstructorAnalyticsDynamicsItemDto>>>;

public sealed record InstructorAnalyticsDynamicsItemDto(
    string Date,
    int Enrollments,
    decimal Earnings);
