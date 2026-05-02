namespace Learnix.Infrastructure.AiChat.Anthropic;

public sealed class AnthropicSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "claude-haiku-4-5-20251001";
    public int MaxTokens { get; init; } = 1024;
    public string BaseUrl { get; init; } = "https://api.anthropic.com";
}
