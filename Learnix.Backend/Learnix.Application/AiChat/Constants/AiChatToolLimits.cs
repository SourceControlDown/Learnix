namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// Caps on tool payload size. Tool results are persisted into the chat session and replayed inside the
/// sliding context window on every subsequent turn (ADR-CHAT-005), so an uncapped list is paid for repeatedly.
/// </summary>
public static class AiChatToolLimits
{
    public const int LearningProfileSectionItems = 15;
    public const int InstructorCourses = 20;
    public const int InstructorCandidates = 10;
    public const int InstructorNameMaxLength = 100;
    public const int CourseDescriptionPreviewLength = 200;
    public const int LessonContentMaxLength = 8000;

    /// <summary>
    /// Up to this many visible lessons, the course outline in the tutor's system prompt lists every lesson
    /// title. Above it, only the current section and its neighbours keep their titles — the outline is paid
    /// for on every request, so a 200-lesson course must not dominate the prompt (ADR-CHAT-013).
    /// </summary>
    public const int CourseOutlineExpandedLessons = 60;

    /// <summary>How many sections on each side of the current one stay expanded in a collapsed outline.</summary>
    public const int CourseOutlineNeighbourSections = 1;
}
