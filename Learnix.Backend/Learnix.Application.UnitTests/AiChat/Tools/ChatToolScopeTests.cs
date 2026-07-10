using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Tools;
using MediatR;

namespace Learnix.Application.UnitTests.AiChat.Tools;

/// <summary>
/// The tutor must not be able to reach platform tools, and the platform assistant must not be able to read
/// lessons. A tool absent from the request cannot be called at all — that is the whole defence.
/// </summary>
public class ChatToolScopeTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private IChatTool[] AllTools() =>
    [
        new SearchCoursesTool(_mediator),
        new GetCategoriesTool(_mediator),
        new GetInstructorCoursesTool(_mediator),
        new GetMyLearningProfileTool(_mediator),
        new GetCurrentLessonTool(_mediator),
        new GetPlatformInfoTool(),
    ];

    private string[] NamesFor(ChatScopeType scope) =>
        AllTools().Where(t => t.IsAvailableIn(scope)).Select(t => t.Name).ToArray();

    [Fact]
    public void Platform_scope_is_never_offered_the_lesson_reader()
    {
        NamesFor(ChatScopeType.Platform).Should().NotContain("get_current_lesson");
    }

    [Fact]
    public void Course_scope_is_never_offered_course_discovery_or_the_learning_profile()
    {
        NamesFor(ChatScopeType.Course).Should().NotContain([
            "search_courses",
            "get_categories",
            "get_instructor_courses",
            "get_my_learning_profile",
        ]);
    }

    [Fact]
    public void Course_scope_offers_the_lesson_reader_and_platform_reference()
    {
        NamesFor(ChatScopeType.Course).Should().BeEquivalentTo(["get_current_lesson", "get_platform_info"]);
    }

    [Fact]
    public void The_lesson_reader_takes_no_arguments_from_the_model()
    {
        var schema = new GetCurrentLessonTool(_mediator).Definition.ParametersJsonSchema;

        schema.Should().NotContain("lessonId");
        schema.Should().NotContain("courseId");
    }

    [Fact]
    public async Task The_lesson_reader_refuses_when_no_lesson_is_open()
    {
        var tool = new GetCurrentLessonTool(_mediator);

        var result = await tool.ExecuteAsync(
            new ChatToolInvocation("{}", new ChatToolContext(Guid.NewGuid(), null)),
            CancellationToken.None);

        result.Should().Contain("error");
        await _mediator.DidNotReceiveWithAnyArgs().Send(default!, default);
    }
}
