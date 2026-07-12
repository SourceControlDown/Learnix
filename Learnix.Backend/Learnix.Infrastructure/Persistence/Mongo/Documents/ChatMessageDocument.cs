namespace Learnix.Infrastructure.Persistence.Mongo.Documents;

public sealed class ChatMessageDocument
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<ToolCallDocument>? ToolCalls { get; set; }
    public DateTime SentAt { get; set; }

    /// <summary>The lesson this message was asked from. Only ever set in a course-scoped session.</summary>
    public Guid? LessonId { get; set; }
}

public sealed class ToolCallDocument
{
    public string CallId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
}
