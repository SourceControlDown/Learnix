using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Queries.GetCourseContextForAi;

/// <summary>
/// Everything the tutor should know about the course without asking: what it is, how it is structured, and
/// how far the student has got. Rendered into the system prompt by <c>ChatSystemPrompt</c>, so it is paid for
/// once per request and never accumulates in the conversation — unlike a tool result (ADR-BACK-CHAT-013).
/// </summary>
/// <param name="Description">Truncated to <c>AiChatToolLimits.CourseDescriptionPreviewLength</c>.</param>
/// <param name="OutlineCollapsed">
/// True when the course was too large to list in full and only the sections around the current one kept
/// their lesson titles.
/// </param>
public sealed record CourseContextForAiDto(
    Guid CourseId,
    string Title,
    string Description,
    string Category,
    string Instructor,
    int TotalLessons,
    int CompletedLessons,
    bool OutlineCollapsed,
    IReadOnlyList<OutlineSectionDto> Sections);

/// <param name="Lessons">Null when the section is collapsed: its lesson titles were dropped to save context.</param>
public sealed record OutlineSectionDto(
    int Number,
    string Title,
    int LessonCount,
    IReadOnlyList<OutlineLessonDto>? Lessons);

public sealed record OutlineLessonDto(
    string Title,
    LessonType Type,
    bool IsCompleted,
    bool IsCurrent);
