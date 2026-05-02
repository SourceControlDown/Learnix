using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Learnix.Infrastructure.AiChat.Anthropic;

internal sealed class AnthropicChatProvider(
    HttpClient httpClient,
    IOptions<AnthropicSettings> options) : IAiChatProvider
{
    private readonly AnthropicSettings _settings = options.Value;

    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var request = AnthropicRequestBuilder.Build(_settings.Model, _settings.MaxTokens, conversation, tools);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages");
        httpRequest.Content = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            yield return new ProviderErrorEvent(
                $"Anthropic API error {(int)response.StatusCode}: {errorBody}",
                "PROVIDER_ERROR");
            yield break;
        }

        var stream = await response.Content.ReadAsStreamAsync(ct);

        await foreach (var evt in AnthropicSseParser.ParseAsync(stream, ct))
            yield return evt;
    }
}
