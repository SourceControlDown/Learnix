namespace Learnix.Application.AiChat.Queries.GetChatSession;

public sealed record ChatSessionDto(
    string SessionId,
    IReadOnlyList<ChatMessageDto> Messages);

public sealed record ChatMessageDto(
    string Role,
    string Content,
    DateTime SentAt);
