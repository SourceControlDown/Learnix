namespace Learnix.Application.AiChat.Abstractions.Models;

public enum ChatScopeType
{
    /// <summary>The site-wide assistant: course discovery, platform questions, the user's own profile.</summary>
    Platform,

    /// <summary>The tutor of one course the user is enrolled in.</summary>
    Course
}

/// <summary>
/// What a chat session is about. Together with the user id it identifies the session — see ADR-CHAT-004.
/// </summary>
public sealed record ChatScope
{
    private ChatScope(ChatScopeType type, Guid? courseId)
    {
        Type = type;
        CourseId = courseId;
    }

    public ChatScopeType Type { get; }

    /// <summary>Set only for <see cref="ChatScopeType.Course"/>.</summary>
    public Guid? CourseId { get; }

    public static ChatScope Platform { get; } = new(ChatScopeType.Platform, null);

    public static ChatScope ForCourse(Guid courseId) => new(ChatScopeType.Course, courseId);
}
