using System.Text.Json;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetCategories;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetCategoriesTool(IMediator mediator) : IChatTool
{
    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    });

    public string Name => ChatToolNames.GetCategories;

    public ToolDefinition Definition => new(
        Name: Name,
        Description: "Returns all available course categories with their slugs and course counts. " +
                     "Call this when the user mentions a subject area and you need the correct category slug " +
                     "to pass to search_courses.",
        ParametersJsonSchema: ParametersSchema);

    public bool IsAvailableIn(ChatScopeType scope) => scope is ChatScopeType.Platform;

    public async Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), cancellationToken);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = "Failed to retrieve categories" });

        return JsonSerializer.Serialize(new { categories = result.Value });
    }
}
