using FluentResults;
using MediatR;

namespace Learnix.Application.Enrollments.Queries.GetContinueLearning;

/// <summary>Resolves to null when the student has no course in progress.</summary>
public sealed record GetContinueLearningQuery : IRequest<Result<ContinueLearningDto?>>;
