using System.Text.Json.Serialization;

namespace Learnix.Infrastructure.AiChat.Anthropic.Dto;

internal sealed record AnthropicRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("messages")] List<AnthropicMessage> Messages,
    [property: JsonPropertyName("system")] string? System = null,
    [property: JsonPropertyName("tools")] List<AnthropicTool>? Tools = null,
    [property: JsonPropertyName("stream")] bool Stream = true);

internal sealed record AnthropicMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] object Content);

internal sealed record AnthropicTool(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("input_schema")] object InputSchema);

internal sealed record AnthropicToolResultBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("tool_use_id")] string ToolUseId,
    [property: JsonPropertyName("content")] string Content);

internal sealed record AnthropicToolUseBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("input")] object Input);

// SSE event data shapes
internal sealed record SseEventData(
    [property: JsonPropertyName("type")] string Type);

internal sealed record ContentBlockStartData(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("content_block")] ContentBlock ContentBlock);

internal sealed record ContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("text")] string? Text);

internal sealed record ContentBlockDeltaData(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("delta")] ContentDelta Delta);

internal sealed record ContentDelta(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("partial_json")] string? PartialJson);
