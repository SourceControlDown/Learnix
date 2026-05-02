using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Learnix.Infrastructure.AiChat.Gemini;

internal sealed class GeminiChatProvider : IAiChatProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;

    public GeminiChatProvider(HttpClient httpClient, IOptions<GeminiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(
        IReadOnlyList<ChatMessage> conversation,
        IReadOnlyList<ToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var request = GeminiRequestBuilder.Build(conversation, tools);

        var url = $"/v1beta/models/{_settings.Model}:streamGenerateContent?key={_settings.ApiKey}&alt=sse";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            yield return new ProviderErrorEvent(
                $"Gemini API error {(int)response.StatusCode}: {errorBody}",
                "PROVIDER_ERROR");
            yield break;
        }

        var stream = await response.Content.ReadAsStreamAsync(ct);

        await foreach (var evt in GeminiSseParser.ParseAsync(stream, ct))
            yield return evt;
    }
}
