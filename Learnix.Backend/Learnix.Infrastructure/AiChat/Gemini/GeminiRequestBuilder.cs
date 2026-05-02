using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Infrastructure.AiChat.Gemini.Dto;
using System.Text.Json;

namespace Learnix.Infrastructure.AiChat.Gemini;

internal static class GeminiRequestBuilder
{
    public static GeminiRequest Build(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools)
    {
        var contents = BuildContents(conversation);
        var geminiTools = tools.Count > 0 ? BuildTools(tools) : null;

        return new GeminiRequest(contents, geminiTools);
    }

    private static List<GeminiContent> BuildContents(IReadOnlyList<ChatMessage> conversation)
    {
        var contents = new List<GeminiContent>();

        foreach (var msg in conversation)
        {
            // Map roles: "user" → "user", "assistant" → "model", "tool_result" → "user"
            if (msg.Role == "tool_result")
            {
                var parts = msg.ToolCalls!.Select(tc =>
                {
                    var responseObj = tc.ResultJson is not null
                        ? JsonSerializer.Deserialize<JsonElement>(tc.ResultJson)
                        : (object)new { };
                    return new GeminiPart(FunctionResponse: new GeminiFunctionResponse(tc.ToolName, responseObj!));
                }).ToList();

                contents.Add(new GeminiContent("user", parts));
            }
            else if (msg.Role == "assistant" && msg.ToolCalls is { Count: > 0 })
            {
                var parts = new List<GeminiPart>();

                if (!string.IsNullOrEmpty(msg.Content))
                    parts.Add(new GeminiPart(Text: msg.Content));

                foreach (var tc in msg.ToolCalls)
                {
                    var argsObj = tc.ArgumentsJson.Length > 0
                        ? JsonSerializer.Deserialize<JsonElement>(tc.ArgumentsJson)
                        : (object)new { };
                    parts.Add(new GeminiPart(FunctionCall: new GeminiFunctionCall(tc.ToolName, argsObj!)));
                }

                contents.Add(new GeminiContent("model", parts));
            }
            else
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new GeminiContent(role, [new GeminiPart(Text: msg.Content)]));
            }
        }

        return contents;
    }

    private static List<GeminiTools> BuildTools(IReadOnlyList<ToolDefinition> tools)
    {
        var declarations = tools.Select(t => new GeminiFunctionDeclaration(
            t.Name,
            t.Description,
            JsonSerializer.Deserialize<JsonElement>(t.ParametersJsonSchema))).ToList();

        return [new GeminiTools(declarations)];
    }
}
