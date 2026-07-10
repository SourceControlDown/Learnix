using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetTestReviewForAi;

/// <summary>
/// The student's most recent submitted attempt at the test they have open. Ids come from the request,
/// never from the model, and are re-checked in the handler.
/// </summary>
public sealed record GetTestReviewForAiQuery(Guid CourseId, Guid LessonId)
    : IRequest<Result<TestReviewForAiDto>>;
