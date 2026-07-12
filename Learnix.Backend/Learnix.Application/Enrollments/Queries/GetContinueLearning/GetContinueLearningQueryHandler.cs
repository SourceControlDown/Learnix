using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using MediatR;

namespace Learnix.Application.Enrollments.Queries.GetContinueLearning;

/// <summary>
/// Picks the course the student was last active in and the lesson they should resume at.
/// Nothing is stored to answer this — both are derived from existing progress rows.
/// </summary>
public sealed class GetContinueLearningQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonProgressRepository lessonProgressRepository,
    ILessonRepository lessonRepository)
    : IRequestHandler<GetContinueLearningQuery, Result<ContinueLearningDto?>>
{
    public async Task<Result<ContinueLearningDto?>> Handle(
        GetContinueLearningQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var enrollments = await enrollmentRepository.ListAsync(
            new ActiveEnrollmentsSpecification(studentId), cancellationToken);

        // Course is a required relationship, so the global soft-delete filter drops the whole
        // enrollment row rather than nulling the navigation. The guard only satisfies the compiler.
        var active = enrollments.Where(e => e.Course is not null).ToList();

        if (active.Count == 0)
            return Result.Ok<ContinueLearningDto?>(null);

        var lastActivity = await lessonProgressRepository.GetLastActivityByCourseAsync(
            studentId, active.Select(e => e.CourseId).ToList(), cancellationToken);

        // A course the student has never touched has no activity timestamp; the enrollment date
        // then decides, which is why the specification already orders by it.
        var chosen = active
            .OrderByDescending(e => lastActivity.TryGetValue(e.CourseId, out var at) ? at : DateTime.MinValue)
            .ThenByDescending(e => e.EnrolledAt)
            .First();

        var lessonId = await lessonRepository.GetResumeLessonIdAsync(
            studentId, chosen.CourseId, cancellationToken);

        // A course whose lessons are all hidden has nothing to open.
        if (lessonId is null)
            return Result.Ok<ContinueLearningDto?>(null);

        return Result.Ok<ContinueLearningDto?>(
            new ContinueLearningDto(chosen.CourseId, chosen.Course!.Title, lessonId.Value));
    }
}
