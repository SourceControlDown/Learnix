using System.Text.Json;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetTestReviewForAi;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetTestReviewTool(IMediator mediator) : IChatTool
{
    // No parameters: the test and the attempt are decided by the request, not by the model.
    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    });

    public string Name => ChatToolNames.GetMyTestReview;

    public ToolDefinition Definition => new(
        Name: Name,
        Description:
            "Returns the student's most recently submitted attempt at the test they have open: every question, " +
            "the options with the correct one marked, what the student answered, and whether they got it right. " +
            "Call this only when the student has already submitted the test and wants to go over their results — " +
            "for example 'why was question 3 wrong', 'what did I get wrong', 'explain my mistakes'. " +
            "Takes no arguments. It returns an error, which you must relay, when the student has not submitted " +
            "the test yet or has an attempt open right now; in that case never guess or reveal any answer.",
        ParametersJsonSchema: ParametersSchema);

    public bool IsAvailableIn(ChatScopeType scope) => scope is ChatScopeType.Course;

    public async Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken cancellationToken)
    {
        var (courseId, lessonId) = invocation.Context;

        if (courseId is null || lessonId is null)
            return JsonSerializer.Serialize(new { error = "The student does not have a test open." });

        var result = await mediator.Send(new GetTestReviewForAiQuery(courseId.Value, lessonId.Value), cancellationToken);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = result.Errors[0].Message });

        return JsonSerializer.Serialize(new { testReview = result.Value }, ChatToolJson.Write);
    }
}
