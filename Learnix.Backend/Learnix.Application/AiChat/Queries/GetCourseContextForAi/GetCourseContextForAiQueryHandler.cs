using FluentResults;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetCourseContextForAi;

internal sealed class GetCourseContextForAiQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository,
    ICategoryRepository categoryRepository,
    IUserRepository userRepository,
    ILessonRepository lessonRepository)
    : IRequestHandler<GetCourseContextForAiQuery, Result<CourseContextForAiDto>>
{
    public async Task<Result<CourseContextForAiDto>> Handle(
        GetCourseContextForAiQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var isEnrolled = await enrollmentRepository.AnyAsync(
            new ActiveEnrollmentByStudentAndCourseSpecification(studentId, request.CourseId),
            cancellationToken);

        if (!isEnrolled)
            return Result.Fail(new ForbiddenError(CommonMessages.NotEnrolledInCourse));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, includeLessons: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        var category = await categoryRepository.FirstOrDefaultAsync(
            new CategoryByIdSpecification(course.CategoryId), cancellationToken);

        var instructor = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(course.InstructorId), cancellationToken);

        var completion = await lessonRepository.GetVisibleLessonCompletionAsync(
            studentId, course.Id, cancellationToken);

        var completedIds = completion.Where(c => c.IsCompleted).Select(c => c.LessonId).ToHashSet();

        var sections = course.Sections
            .OrderBy(s => s.DisplayOrder)
            .Select(s => s.Lessons.Where(l => !l.IsHidden).OrderBy(l => l.DisplayOrder).ToList())
            .ToList();

        var totalLessons = sections.Sum(l => l.Count);
        var collapsed = totalLessons > AiChatToolLimits.CourseOutlineExpandedLessons;
        var currentSection = IndexOfSectionWith(sections, request.LessonId);

        var outline = course.Sections
            .OrderBy(s => s.DisplayOrder)
            .Select((section, index) => new OutlineSectionDto(
                Number: index + 1,
                Title: section.Title,
                LessonCount: sections[index].Count,
                Lessons: collapsed && !IsNearCurrent(index, currentSection)
                    ? null
                    : sections[index]
                        .Select(l => new OutlineLessonDto(
                            l.Title,
                            l.LessonType,
                            completedIds.Contains(l.Id),
                            l.Id == request.LessonId))
                        .ToList()))
            .ToList();

        return Result.Ok(new CourseContextForAiDto(
            course.Id,
            course.Title,
            Truncate(course.Description, AiChatToolLimits.CourseDescriptionPreviewLength),
            category?.Name ?? string.Empty,
            instructor is not null ? $"{instructor.FirstName} {instructor.LastName}" : string.Empty,
            totalLessons,
            completedIds.Count,
            collapsed,
            outline));
    }

    private static int IndexOfSectionWith(List<List<Lesson>> sections, Guid? lessonId) =>
        lessonId is null
            ? 0
            : Math.Max(0, sections.FindIndex(lessons => lessons.Any(l => l.Id == lessonId.Value)));

    /// <summary>
    /// Which sections keep their lesson titles once the course is too big to list in full: the one the
    /// student is in, plus its neighbours — that is the span a "what comes next" question ever reaches for.
    /// </summary>
    private static bool IsNearCurrent(int index, int currentSection) =>
        Math.Abs(index - currentSection) <= AiChatToolLimits.CourseOutlineNeighbourSections;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
