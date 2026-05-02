using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Infrastructure.AiChat.Gemini.Dto;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Learnix.Infrastructure.AiChat.Gemini;

internal static class GeminiSseParser
{
    public static async IAsyncEnumerable<ChatStreamEvent> ParseAsync(
        Stream responseStream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(responseStream);

        string? dataLine = null;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (line.StartsWith("data:"))
            {
                dataLine = line["data:".Length..].Trim();
                continue;
            }

            if (line.Length == 0 && dataLine is not null)
            {
                await foreach (var evt in ProcessChunkAsync(dataLine, ct))
                    yield return evt;

                dataLine = null;
            }
        }

        // Handle last chunk if no trailing newline
        if (dataLine is not null)
        {
            await foreach (var evt in ProcessChunkAsync(dataLine, ct))
                yield return evt;
        }

        yield return new MessageEndEvent("stop");
    }

    private static async IAsyncEnumerable<ChatStreamEvent> ProcessChunkAsync(
        string data,
        [EnumeratorCancellation] CancellationToken ct)
    {
        GeminiSseChunk? chunk;
        try
        {
            chunk = JsonSerializer.Deserialize<GeminiSseChunk>(data);
        }
        catch
        {
            yield break;
        }

        if (chunk?.Candidates is null) yield break;

        foreach (var candidate in chunk.Candidates)
        {
            if (candidate.Content?.Parts is null) continue;

            foreach (var part in candidate.Content.Parts)
            {
                if (part.Text is not null)
                {
                    yield return new TextDeltaEvent(part.Text);
                }
                else if (part.FunctionCall is not null)
                {
                    var callId = Guid.NewGuid().ToString("N")[..8];
                    var argsJson = JsonSerializer.Serialize(part.FunctionCall.Args);

                    yield return new ToolUseStartEvent(callId, part.FunctionCall.Name);
                    yield return new ToolUseEndEvent(callId, part.FunctionCall.Name, argsJson);
                }
            }

            if (candidate.FinishReason is "STOP" or "MAX_TOKENS")
                yield return new MessageEndEvent(candidate.FinishReason);
        }

        await Task.CompletedTask;
    }
}
