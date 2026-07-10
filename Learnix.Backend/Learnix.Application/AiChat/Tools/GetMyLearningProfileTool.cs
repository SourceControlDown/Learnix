using System.Text.Json;
using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetMyLearningProfile;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetMyLearningProfileTool(IMediator mediator) : IChatTool
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions WriteOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            sections = new
            {
                type = "array",
                description =
                    "Which parts of the profile to return. Omit to get everything. " +
                    "Request only what you need — each section costs tokens.",
                items = new { type = "string", @enum = LearningProfileSections.All }
            }
        },
        required = Array.Empty<string>()
    });

    public string Name => ChatToolNames.GetMyLearningProfile;

    public ToolDefinition Definition => new(
        Name: Name,
        Description:
            "Returns the signed-in user's own profile and learning state: personal details, courses in " +
            "progress with completion percentage, finished courses, wishlist, and unlocked achievements. " +
            "Call this whenever the user asks about themselves — what they are studying, what they finished, " +
            "what they saved for later, how far along they are — or when you need their interests to give a " +
            "personalised course recommendation. It always describes the current user and nobody else.",
        ParametersJsonSchema: ParametersSchema);

    public bool IsAvailableIn(ChatScopeType scope) => scope is ChatScopeType.Platform;

    public async Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken ct)
    {
        var argumentsJson = invocation.ArgumentsJson;
        ProfileArgs? args;
        try
        {
            args = JsonSerializer.Deserialize<ProfileArgs>(argumentsJson, ReadOptions);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { error = "Invalid arguments" });
        }

        var result = await mediator.Send(new GetMyLearningProfileQuery(args?.Sections), ct);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = result.Errors[0].Message });

        return JsonSerializer.Serialize(new { learningProfile = result.Value }, WriteOptions);
    }

    private sealed record ProfileArgs(
        [property: JsonPropertyName("sections")] IReadOnlyList<string>? Sections);
}
