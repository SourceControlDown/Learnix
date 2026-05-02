using System.Text.Json.Serialization;

namespace Learnix.Infrastructure.AiChat.Gemini.Dto;

internal sealed record GeminiRequest(
    [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
    [property: JsonPropertyName("tools")] List<GeminiTools>? Tools = null,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig? GenerationConfig = null);

internal sealed record GeminiContent(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

internal sealed record GeminiPart(
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("functionCall")] GeminiFunctionCall? FunctionCall = null,
    [property: JsonPropertyName("functionResponse")] GeminiFunctionResponse? FunctionResponse = null);

internal sealed record GeminiFunctionCall(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("args")] object Args);

internal sealed record GeminiFunctionResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("response")] object Response);

internal sealed record GeminiTools(
    [property: JsonPropertyName("functionDeclarations")] List<GeminiFunctionDeclaration> FunctionDeclarations);

internal sealed record GeminiFunctionDeclaration(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] object Parameters);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens = 1024);

// SSE response shapes
internal sealed record GeminiSseChunk(
    [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content,
    [property: JsonPropertyName("finishReason")] string? FinishReason);
