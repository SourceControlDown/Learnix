using System.Text.Json;
using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Queries.GetCoursesByInstructor;
using MediatR;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetInstructorCoursesTool(IMediator mediator) : IChatTool
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions WriteOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            instructorName = new
            {
                type = "string",
                description = "Full or partial instructor name, e.g. 'Olena' or 'Olena Kovalchuk'."
            },
            instructorId = new
            {
                type = "string",
                description = "Instructor GUID. Use it when you already know the id, for example from search_courses results."
            }
        },
        required = Array.Empty<string>()
    });

    public string Name => "get_instructor_courses";

    public ToolDefinition Definition => new(
        Name: "get_instructor_courses",
        Description:
            "Returns an instructor and their published courses. Pass instructorName when the user names a " +
            "person, or instructorId when you already have it. If several instructors match the name, the " +
            "result contains an 'Ambiguous' list instead of courses — show those names and ask the user which " +
            "one they meant, then call again with the chosen instructorId.",
        ParametersJsonSchema: ParametersSchema);

    public async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct)
    {
        InstructorArgs? args;
        try
        {
            args = JsonSerializer.Deserialize<InstructorArgs>(argumentsJson, ReadOptions);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { error = "Invalid arguments" });
        }

        Guid? instructorId = null;
        if (!string.IsNullOrWhiteSpace(args?.InstructorId))
        {
            if (!Guid.TryParse(args.InstructorId, out var parsed))
                return JsonSerializer.Serialize(new { error = "instructorId is not a valid GUID" });

            instructorId = parsed;
        }

        var result = await mediator.Send(
            new GetCoursesByInstructorQuery(args?.InstructorName, instructorId), ct);

        if (result.IsFailed)
            return JsonSerializer.Serialize(new { error = result.Errors[0].Message });

        return JsonSerializer.Serialize(result.Value, WriteOptions);
    }

    private sealed record InstructorArgs(
        [property: JsonPropertyName("instructorName")] string? InstructorName,
        [property: JsonPropertyName("instructorId")] string? InstructorId);
}
