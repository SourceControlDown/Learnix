namespace Learnix.Application.Common.Settings;

public sealed class AiChatSettings
{
    public int MessagesPerSessionCap { get; init; } = 50;
    public int ContextWindowSize { get; init; } = 20;
}
