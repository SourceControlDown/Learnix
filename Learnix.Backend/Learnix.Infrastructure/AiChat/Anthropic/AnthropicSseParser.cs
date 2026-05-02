using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Infrastructure.AiChat.Anthropic.Dto;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Learnix.Infrastructure.AiChat.Anthropic;

internal static class AnthropicSseParser
{
    public static async IAsyncEnumerable<ChatStreamEvent> ParseAsync(
        Stream responseStream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(responseStream);

        // Track tool use blocks being accumulated per index
        var toolBlocks = new Dictionary<int, (string CallId, string Name, System.Text.StringBuilder JsonAccumulator)>();

        string? eventType = null;
        string? dataLine = null;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);

            if (line is null) break;

            if (line.StartsWith("event:"))
            {
                eventType = line["event:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("data:"))
            {
                dataLine = line["data:".Length..].Trim();
                continue;
            }

            // Empty line = end of event
            if (line.Length == 0 && eventType is not null && dataLine is not null)
            {
                var evt = ProcessEvent(eventType, dataLine, toolBlocks);
                if (evt is not null)
                    yield return evt;

                eventType = null;
                dataLine = null;
            }
        }
    }

    private static ChatStreamEvent? ProcessEvent(
        string eventType,
        string data,
        Dictionary<int, (string CallId, string Name, System.Text.StringBuilder JsonAccumulator)> toolBlocks)
    {
        try
        {
            return eventType switch
            {
                "content_block_start" => HandleContentBlockStart(data, toolBlocks),
                "content_block_delta" => HandleContentBlockDelta(data, toolBlocks),
                "content_block_stop" => HandleContentBlockStop(data, toolBlocks),
                "message_stop" => new MessageEndEvent("end_turn"),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static ChatStreamEvent? HandleContentBlockStart(
        string data,
        Dictionary<int, (string CallId, string Name, System.Text.StringBuilder JsonAccumulator)> toolBlocks)
    {
        var parsed = JsonSerializer.Deserialize<ContentBlockStartData>(data);
        if (parsed?.ContentBlock is null) return null;

        if (parsed.ContentBlock.Type == "tool_use")
        {
            toolBlocks[parsed.Index] = (
                parsed.ContentBlock.Id ?? string.Empty,
                parsed.ContentBlock.Name ?? string.Empty,
                new System.Text.StringBuilder());

            return new ToolUseStartEvent(
                parsed.ContentBlock.Id ?? string.Empty,
                parsed.ContentBlock.Name ?? string.Empty);
        }

        return null;
    }

    private static ChatStreamEvent? HandleContentBlockDelta(
        string data,
        Dictionary<int, (string CallId, string Name, System.Text.StringBuilder JsonAccumulator)> toolBlocks)
    {
        var parsed = JsonSerializer.Deserialize<ContentBlockDeltaData>(data);
        if (parsed?.Delta is null) return null;

        if (parsed.Delta.Type == "text_delta" && parsed.Delta.Text is not null)
            return new TextDeltaEvent(parsed.Delta.Text);

        if (parsed.Delta.Type == "input_json_delta" && parsed.Delta.PartialJson is not null)
        {
            if (toolBlocks.TryGetValue(parsed.Index, out var block))
                block.JsonAccumulator.Append(parsed.Delta.PartialJson);
        }

        return null;
    }

    private static ChatStreamEvent? HandleContentBlockStop(
        string data,
        Dictionary<int, (string CallId, string Name, System.Text.StringBuilder JsonAccumulator)> toolBlocks)
    {
        var parsed = JsonSerializer.Deserialize<JsonElement>(data);
        if (!parsed.TryGetProperty("index", out var indexEl)) return null;

        var index = indexEl.GetInt32();
        if (!toolBlocks.TryGetValue(index, out var block)) return null;

        var argumentsJson = block.JsonAccumulator.ToString();
        toolBlocks.Remove(index);

        return new ToolUseEndEvent(block.CallId, block.Name, argumentsJson);
    }
}
