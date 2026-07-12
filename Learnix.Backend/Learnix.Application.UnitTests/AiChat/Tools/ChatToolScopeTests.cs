using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Services;
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
        new GetTestReviewTool(_mediator),
        new GetPlatformInfoTool(),
    ];

    private string[] NamesFor(ChatScopeType scope) =>
        AllTools().Where(t => t.IsAvailableIn(scope)).Select(t => t.Name).ToArray();

    [Fact]
    public void Platform_scope_is_never_offered_the_lesson_reader_or_the_test_review()
    {
        NamesFor(ChatScopeType.Platform).Should().NotContain(
            [ChatToolNames.GetCurrentLesson, ChatToolNames.GetMyTestReview]);
    }

    [Fact]
    public void Course_scope_is_never_offered_course_discovery_or_the_learning_profile()
    {
        NamesFor(ChatScopeType.Course).Should().NotContain([
            ChatToolNames.SearchCourses,
            ChatToolNames.GetCategories,
            ChatToolNames.GetInstructorCourses,
            ChatToolNames.GetMyLearningProfile,
        ]);
    }

    [Fact]
    public void Course_scope_offers_the_lesson_reader_the_test_review_and_platform_reference()
    {
        NamesFor(ChatScopeType.Course).Should().BeEquivalentTo(
            [ChatToolNames.GetCurrentLesson, ChatToolNames.GetMyTestReview, ChatToolNames.GetPlatformInfo]);
    }

    [Theory]
    [InlineData(ChatToolNames.GetCurrentLesson)]
    [InlineData(ChatToolNames.GetMyTestReview)]
    public void The_lesson_scoped_tools_take_no_arguments_from_the_model(string toolName)
    {
        var schema = AllTools().Single(t => t.Name == toolName).Definition.ParametersJsonSchema;

        schema.Should().NotContain("lessonId");
        schema.Should().NotContain("courseId");
        schema.Should().NotContain("attemptId");
    }

    [Fact]
    public void Every_tool_reports_the_name_it_advertises_in_its_definition()
    {
        foreach (var tool in AllTools())
            tool.Definition.Name.Should().Be(tool.Name);
    }

    [Theory]
    [InlineData(ChatScopeType.Platform)]
    [InlineData(ChatScopeType.Course)]
    public void The_prompt_advertises_exactly_the_tools_the_scope_hands_the_provider(ChatScopeType scope)
    {
        var chatScope = scope is ChatScopeType.Course ? ChatScope.ForCourse(Guid.NewGuid()) : ChatScope.Platform;
        var prompt = ChatSystemPrompt.For(chatScope, lessonId: null);

        foreach (var tool in AllTools())
        {
            // Naming a tool the scope withholds invites a call that cannot succeed; omitting one it offers
            // leaves the model unaware the tool exists.
            prompt.Contains(tool.Name, StringComparison.Ordinal)
                .Should().Be(tool.IsAvailableIn(scope), $"prompt for {scope} vs tool {tool.Name}");
        }
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
