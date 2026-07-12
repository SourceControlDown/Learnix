namespace Learnix.Application.AiChat.Abstractions.Models;

/// <param name="LessonId">
/// The lesson the user was on when the message was sent. Set on user messages of a course-scoped session,
/// null everywhere else. Persisted so a replayed window shows which lesson each turn was about.
/// </param>
public sealed record ChatMessage(
    string Role,
    string Content,
    DateTime SentAt,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    Guid? LessonId = null);
