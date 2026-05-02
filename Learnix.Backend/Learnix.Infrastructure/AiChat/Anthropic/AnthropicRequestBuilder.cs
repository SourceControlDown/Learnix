using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Infrastructure.AiChat.Anthropic.Dto;
using System.Text.Json;

namespace Learnix.Infrastructure.AiChat.Anthropic;

internal static class AnthropicRequestBuilder
{
    private static readonly string SystemPrompt =
        "You are a helpful learning assistant for Learnix, an online learning platform. " +
        "You help students find courses and answer questions about learning topics. " +
        "When a user asks for course recommendations, use the search_courses tool to find relevant courses. " +
        "Be concise and friendly.";

    public static AnthropicRequest Build(
        string model,
        int maxTokens,
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools)
    {
        var messages = BuildMessages(conversation);
        var anthropicTools = tools.Count > 0 ? BuildTools(tools) : null;

        return new AnthropicRequest(
            Model: model,
            MaxTokens: maxTokens,
            Messages: messages,
            System: SystemPrompt,
            Tools: anthropicTools);
    }

    private static List<AnthropicMessage> BuildMessages(IReadOnlyList<ChatMessage> conversation)
    {
        var messages = new List<AnthropicMessage>();

        foreach (var msg in conversation)
        {
            if (msg.Role == "tool_result")
            {
                // tool_result messages: content is a list of tool_result blocks
                var toolResultBlocks = msg.ToolCalls!
                    .Select(tc => new AnthropicToolResultBlock("tool_result", tc.CallId, tc.ResultJson ?? ""))
                    .ToList();

                messages.Add(new AnthropicMessage("user", toolResultBlocks));
            }
            else if (msg.Role == "assistant" && msg.ToolCalls is { Count: > 0 })
            {
                // assistant message that triggered tool calls: content is a list of blocks
                var blocks = new List<object>();

                if (!string.IsNullOrEmpty(msg.Content))
                    blocks.Add(new { type = "text", text = msg.Content });

                foreach (var tc in msg.ToolCalls)
                {
                    var input = tc.ArgumentsJson.Length > 0
                        ? JsonSerializer.Deserialize<JsonElement>(tc.ArgumentsJson)
                        : (object)new { };
                    blocks.Add(new AnthropicToolUseBlock("tool_use", tc.CallId, tc.ToolName, input));
                }

                messages.Add(new AnthropicMessage("assistant", blocks));
            }
            else
            {
                messages.Add(new AnthropicMessage(msg.Role, msg.Content));
            }
        }

        return messages;
    }

    private static List<AnthropicTool> BuildTools(IReadOnlyList<ToolDefinition> tools) =>
        tools.Select(t => new AnthropicTool(
            t.Name,
            t.Description,
            JsonSerializer.Deserialize<JsonElement>(t.ParametersJsonSchema))).ToList();
}
