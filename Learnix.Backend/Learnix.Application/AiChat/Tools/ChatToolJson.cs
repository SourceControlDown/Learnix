using System.Text.Json;
using System.Text.Json.Serialization;

namespace Learnix.Application.AiChat.Tools;

internal static class ChatToolJson
{
    /// <summary>
    /// How a tool result is written for the model. Enums go out as names: <c>"lessonType": 0</c> is a riddle
    /// the model has to guess at, <c>"lessonType": "Test"</c> is not. Keys are camelCase, matching the field
    /// names the tool descriptions and the system prompt refer to.
    /// </summary>
    public static readonly JsonSerializerOptions Write = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
