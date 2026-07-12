using FluentResults;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Queries.GetTestReviewForAi;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.UnitTests.AiChat.Tools;

/// <summary>Covers the payload the model actually receives, not just the DTO the handler returns.</summary>
public class GetTestReviewToolTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly GetTestReviewTool _sut;

    private static readonly Guid CourseId = Guid.NewGuid();
    private static readonly Guid LessonId = Guid.NewGuid();

    public GetTestReviewToolTests()
    {
        _sut = new GetTestReviewTool(_mediator);
    }

    private Task<string> Act(Guid? courseId, Guid? lessonId) =>
        _sut.ExecuteAsync(
            new ChatToolInvocation("{}", new ChatToolContext(courseId, lessonId)),
            CancellationToken.None);

    [Fact]
    public async Task Refuses_without_asking_the_handler_when_no_lesson_is_open()
    {
        var payload = await Act(CourseId, null);

        payload.Should().Contain("error");
        await _mediator.DidNotReceiveWithAnyArgs().Send(default!, default);
    }

    [Fact]
    public async Task Relays_the_handler_failure_message_verbatim()
    {
        _mediator
            .Send(Arg.Any<GetTestReviewForAiQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<TestReviewForAiDto>(new ConflictError("Attempt still open.")));

        var payload = await Act(CourseId, LessonId);

        payload.Should().Contain("Attempt still open.");
    }

    [Fact]
    public async Task Writes_enum_members_as_names_so_the_model_does_not_have_to_guess()
    {
        _mediator
            .Send(Arg.Any<GetTestReviewForAiQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(Review()));

        var payload = await Act(CourseId, LessonId);

        payload.Should().Contain("\"type\":\"SingleChoice\"");
        payload.Should().NotContain("\"type\":0");
    }

    [Fact]
    public async Task Names_the_fields_the_tool_description_and_the_prompt_refer_to()
    {
        _mediator
            .Send(Arg.Any<GetTestReviewForAiQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(Review()));

        var payload = await Act(CourseId, LessonId);

        payload.Should().Contain("\"testReview\"");
        payload.Should().Contain("\"isCorrect\"");
        payload.Should().Contain("\"studentSelectedOptionOrders\"");
    }

    private static TestReviewForAiDto Review() => new(
        LessonId, "Checkpoint", AttemptNumber: 1, Score: 1, MaxScore: 2, Passed: false,
        SubmittedAt: DateTime.UtcNow,
        Questions:
        [
            new QuestionReviewDto(
                Order: 0,
                Text: "Capital of France",
                Type: QuestionType.SingleChoice,
                Answered: true,
                IsCorrect: false,
                Options: [new OptionReviewDto(0, "Paris", true), new OptionReviewDto(1, "Berlin", false)],
                StudentSelectedOptionOrders: [1])
        ]);
}
