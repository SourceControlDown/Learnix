using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Tools;

public interface IChatTool
{
    string Name { get; }
    ToolDefinition Definition { get; }

    /// <summary>
    /// Whether this tool is offered to the model in the given scope. A tool absent from the request cannot
    /// be called at all, which is a stronger guarantee than forbidding it in the prompt.
    /// </summary>
    bool IsAvailableIn(ChatScopeType scope);

    Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken cancellationToken);
}
