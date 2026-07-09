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
}
