using System.Text.Json;
using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Queries.GetLessonForAi;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetCurrentLessonTool(IMediator mediator) : IChatTool
{
    private static readonly JsonSerializerOptions WriteOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    // No parameters at all: the lesson is decided by the request, not by the model. Accepting a lesson id
    // here would turn the tool into an arbitrary reader for anyone who can get text into the conversation.
    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    });

    public string Name => "get_current_lesson";

    public ToolDefinition Definition => new(
        Name: "get_current_lesson",
        Description:
            "Returns the lesson the student currently has open: its title and type, the written body for a " +
            "post lesson, the instructor's description for a video lesson, and the rules of a test. " +
            "Call it whenever the student refers to 'this lesson', 'this test', 'here', or asks anything that " +
            "depends on the material in front of them. Takes no arguments — it always returns the lesson they " +
            "actually have open. When 'contentAvailable' is false, the substance of the lesson is not " +
            "available to you and 'contentUnavailableReason' explains why; never guess what it contains.",
        ParametersJsonSchema: ParametersSchema);

    public bool IsAvailableIn(ChatScopeType scope) => scope is ChatScopeType.Course;

    public async Task<string> ExecuteAsync(ChatToolInvocation invocation, CancellationToken ct)
    {
        var (courseId, lessonId) = invocation.Context;

        if (courseId is null || lessonId is null)
            return JsonSerializer.Serialize(new { error = "The student does not have a lesson open." });

        var result = await mediator.Send(new GetLessonForAiQuery(courseId.Value, lessonId.Value), ct);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = result.Errors[0].Message });

        return JsonSerializer.Serialize(new { currentLesson = result.Value }, WriteOptions);
    }
}
