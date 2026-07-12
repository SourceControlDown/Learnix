using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetLessonForAi;

/// <summary>
/// Ids come from the request the browser sent, never from the model. Still re-checked in the handler.
/// </summary>
public sealed record GetLessonForAiQuery(Guid CourseId, Guid LessonId) : IRequest<Result<LessonForAiDto>>;
