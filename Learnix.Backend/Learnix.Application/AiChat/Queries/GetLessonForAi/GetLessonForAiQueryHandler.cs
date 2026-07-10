using FluentResults;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetLessonForAi;

internal sealed class GetLessonForAiQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository)
    : IRequestHandler<GetLessonForAiQuery, Result<LessonForAiDto>>
{
    private const string VideoUnavailableReason =
        "This is a video lesson. There is no transcript and the assistant cannot watch video, "
        + "so only the title and the instructor's description are known.";

    private const string TestUnavailableReason =
        "This is a test. Its questions and answers are intentionally withheld from the assistant.";

    public async Task<Result<LessonForAiDto>> Handle(
        GetLessonForAiQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var isEnrolled = await enrollmentRepository.AnyAsync(
            new ActiveEnrollmentByStudentAndCourseSpecification(currentUser.UserId.Value, request.CourseId),
            cancellationToken);

        if (!isEnrolled)
            return Result.Fail(new ForbiddenError(CommonMessages.NotEnrolledInCourse));

        var lesson = await lessonRepository.GetVisibleLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        return Result.Ok(Map(lesson));
    }

    private static LessonForAiDto Map(Lesson lesson) => lesson switch
    {
        VideoLesson v => new LessonForAiDto(
            v.Id, v.Title, v.LessonType,
            ContentAvailable: false,
            Description: v.Description,
            DurationSeconds: v.DurationSeconds,
            ContentUnavailableReason: VideoUnavailableReason),

        PostLesson p => MapPost(p),

        // Questions are never read here — only counted. See ADR-CHAT-012.
        TestLesson t => new LessonForAiDto(
            t.Id, t.Title, t.LessonType,
            ContentAvailable: false,
            Description: t.Description,
            Test: new TestInfoDto(t.Questions.Count, t.PassingThreshold, t.AttemptLimit, t.CooldownMinutes),
            ContentUnavailableReason: TestUnavailableReason),

        _ => throw new InvalidOperationException($"Unknown lesson type for lesson {lesson.Id}.")
    };

    private static LessonForAiDto MapPost(PostLesson post)
    {
        // Tool results are persisted and replayed inside the context window on every later turn, so an
        // uncapped lesson body is paid for repeatedly.
        var truncated = post.Content.Length > AiChatToolLimits.LessonContentMaxLength;

        var content = truncated
            ? post.Content[..AiChatToolLimits.LessonContentMaxLength]
            : post.Content;

        return new LessonForAiDto(
            post.Id, post.Title, post.LessonType,
            ContentAvailable: true,
            Content: content,
            ContentTruncated: truncated);
    }
}
