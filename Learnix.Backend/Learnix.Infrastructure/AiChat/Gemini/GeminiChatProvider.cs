using System.Runtime.CompilerServices;
using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Microsoft.Extensions.Options;
using ChatMessage = Learnix.Application.AiChat.Abstractions.Models.ChatMessage;

namespace Learnix.Infrastructure.AiChat.Gemini;

internal sealed class GeminiChatProvider : IAiChatProvider
{
    private readonly Client _client;
    private readonly GeminiSettings _settings;

    public GeminiChatProvider(IOptions<GeminiSettings> options)
    {
        _settings = options.Value;
        _client = new Client(apiKey: _settings.ApiKey);
    }

    public string Name => "Gemini";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    /// <summary>
    /// The stream is driven by hand rather than with <c>await foreach</c>: a quota refusal surfaces as an
    /// exception out of <c>MoveNextAsync</c>, and an iterator cannot yield from inside a catch. Classifying
    /// it into a <see cref="ProviderErrorEvent"/> is the whole point — a throw here would just kill an SSE
    /// connection whose headers are already out (ADR-CHAT-014).
    /// </summary>
    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var contents = MapContents(request.Conversation);
        var config = BuildConfig(request.Tools, request.SystemPrompt);
        string? finishReason = null;

        var chunks = _client.Models
            .GenerateContentStreamAsync(_settings.Model, contents, config)
            .GetAsyncEnumerator(ct);

        try
        {
            while (true)
            {
                List<ChatStreamEvent> events;
                ChatStreamEvent? failure = null;

                try
                {
                    if (!await chunks.MoveNextAsync())
                        break;

                    events = MapChunk(chunks.Current, ref finishReason);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    events = [];
                    failure = AiProviderErrors.Classify(ex);
                }

                if (failure is not null)
                {
                    yield return failure;
                    yield break;
                }

                foreach (var streamEvent in events)
                    yield return streamEvent;
            }
        }
        finally
        {
            await chunks.DisposeAsync();
        }

        yield return new MessageEndEvent(finishReason ?? "stop");
    }

    private static List<ChatStreamEvent> MapChunk(GenerateContentResponse chunk, ref string? finishReason)
    {
        var events = new List<ChatStreamEvent>();

        if (chunk.Candidates is null)
            return events;

        foreach (var candidate in chunk.Candidates)
        {
            if (candidate.Content?.Parts is not null)
            {
                foreach (var part in candidate.Content.Parts)
                {
                    if (part.Text is not null)
                        events.Add(new TextDeltaEvent(part.Text));

                    if (part.FunctionCall is not null)
                    {
                        var callId = Guid.NewGuid().ToString("N")[..8];
                        var argsJson = JsonSerializer.Serialize(part.FunctionCall.Args);
                        var toolName = part.FunctionCall.Name ?? string.Empty;
                        events.Add(new ToolUseStartEvent(callId, toolName));
                        events.Add(new ToolUseEndEvent(callId, toolName, argsJson));
                    }
                }
            }

            if (candidate.FinishReason is not null)
                finishReason = candidate.FinishReason.ToString();
        }

        return events;
    }

    private static List<Content> MapContents(IReadOnlyList<ChatMessage> conversation)
    {
        var contents = new List<Content>();

        foreach (var msg in conversation)
        {
            if (msg.Role == "tool_result")
            {
                var parts = msg.ToolCalls!.Select(tc =>
                {
                    var response = tc.ResultJson is not null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(tc.ResultJson)
                        : new Dictionary<string, object>();

                    return new Part
                    {
                        FunctionResponse = new FunctionResponse
                        {
                            Name = tc.ToolName,
                            Response = response
                        }
                    };
                }).ToList();

                contents.Add(new Content { Role = "user", Parts = parts });
            }
            else if (msg.Role == "assistant" && msg.ToolCalls is { Count: > 0 })
            {
                var parts = new List<Part>();

                if (!string.IsNullOrEmpty(msg.Content))
                    parts.Add(new Part { Text = msg.Content });

                foreach (var tc in msg.ToolCalls)
                {
                    var args = tc.ArgumentsJson.Length > 0
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(tc.ArgumentsJson)
                        : new Dictionary<string, object>();

                    parts.Add(new Part
                    {
                        FunctionCall = new FunctionCall { Name = tc.ToolName, Args = args }
                    });
                }

                contents.Add(new Content { Role = "model", Parts = parts });
            }
            else
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new Content { Role = role, Parts = [new Part { Text = msg.Content }] });
            }
        }

        return contents;
    }

    private GenerateContentConfig BuildConfig(IReadOnlyList<ToolDefinition> tools, string systemPrompt)
    {
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemPrompt }]
            },
            MaxOutputTokens = _settings.MaxTokens
        };

        if (tools.Count > 0)
        {
            var declarations = tools.Select(t => new FunctionDeclaration
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = JsonSerializer.Deserialize<Schema>(t.ParametersJsonSchema)
            }).ToList();

            config.Tools = [new Tool { FunctionDeclarations = declarations }];
        }

        return config;
    }
}
