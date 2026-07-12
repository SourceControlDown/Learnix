using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetCourseContextForAi;

/// <summary>
/// The course a tutor session is about, as facts for the system prompt — not as a tool result (ADR-CHAT-013).
/// </summary>
/// <param name="LessonId">The lesson the student has open, used to decide which sections stay expanded.</param>
public sealed record GetCourseContextForAiQuery(Guid CourseId, Guid? LessonId)
    : IRequest<Result<CourseContextForAiDto>>;
