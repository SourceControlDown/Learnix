using FluentResults;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Achievements.Specifications;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Constants;
using Learnix.Application.Users.Specifications;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetMyLearningProfile;

internal sealed class GetMyLearningProfileQueryHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IUserRoleService roleService,
    IEnrollmentRepository enrollmentRepository,
    ILessonProgressRepository lessonProgressRepository,
    ICategoryRepository categoryRepository,
    IWishlistRepository wishlistRepository,
    IUserAchievementRepository achievementRepository,
    IUserAchievementProgressRepository achievementProgressRepository)
    : IRequestHandler<GetMyLearningProfileQuery, Result<MyLearningProfileDto>>
{
    private const int Cap = AiChatToolLimits.LearningProfileSectionItems;

    public async Task<Result<MyLearningProfileDto>> Handle(
        GetMyLearningProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var requested = request.Sections is null || request.Sections.Count == 0
            ? LearningProfileSections.All
            : request.Sections;
        var wanted = new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);

        ProfileSummaryDto? profile = null;
        if (wanted.Contains(LearningProfileSections.Profile))
        {
            profile = await BuildProfileAsync(userId, cancellationToken);

            if (profile is null)
                return Result.Fail(new NotFoundError(UserMessages.GenericUserNotFound));
        }

        var wantsInProgress = wanted.Contains(LearningProfileSections.InProgress);
        var wantsCompleted = wanted.Contains(LearningProfileSections.Completed);

        LearningProfileSection<InProgressCourseDto>? inProgress = null;
        LearningProfileSection<CompletedCourseDto>? completed = null;

        if (wantsInProgress || wantsCompleted)
        {
            var enrollments = await enrollmentRepository.ListAsync(
                new StudentEnrollmentsSpecification(userId), cancellationToken);

            // Course is a required relationship, so the global soft-delete filter drops the whole
            // enrollment row rather than nulling the navigation. The guard only satisfies the compiler.
            var visible = enrollments.Where(e => e.Course is not null).ToList();

            var active = visible.Where(e => e.Status == EnrollmentStatus.Active).ToList();
            var finished = visible.Where(e => e.Status == EnrollmentStatus.Completed).ToList();

            var activePage = wantsInProgress ? active.Take(Cap).ToList() : [];
            var finishedPage = wantsCompleted ? finished.Take(Cap).ToList() : [];

            var categoryNames = await ResolveCategoryNamesAsync(
                activePage.Concat(finishedPage).Select(e => e.Course!.CategoryId),
                cancellationToken);

            if (wantsInProgress)
            {
                var counts = await lessonProgressRepository.GetProgressCountsAsync(
                    userId, activePage.Select(e => e.CourseId).ToList(), cancellationToken);

                inProgress = new LearningProfileSection<InProgressCourseDto>(
                    active.Count,
                    active.Count > activePage.Count,
                    activePage.Select(e => ToInProgressDto(e, counts, categoryNames)).ToList());
            }

            if (wantsCompleted)
            {
                completed = new LearningProfileSection<CompletedCourseDto>(
                    finished.Count,
                    finished.Count > finishedPage.Count,
                    finishedPage.Select(e => new CompletedCourseDto(
                        e.CourseId,
                        e.Course!.Title,
                        CategoryNameOf(categoryNames, e.Course.CategoryId),
                        e.CompletedAt)).ToList());
            }
        }

        var wishlist = wanted.Contains(LearningProfileSections.Wishlist)
            ? await BuildWishlistAsync(userId, cancellationToken)
            : null;

        var achievements = wanted.Contains(LearningProfileSections.Achievements)
            ? await BuildAchievementsAsync(userId, cancellationToken)
            : null;

        return Result.Ok(new MyLearningProfileDto(profile, inProgress, completed, wishlist, achievements));
    }

    private async Task<ProfileSummaryDto?> BuildProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.FirstOrDefaultAsync(new UserByIdSpecification(userId), ct);

        if (user is null)
            return null;

        var roles = await roleService.GetRolesAsync(userId, ct);

        return new ProfileSummaryDto(
            user.FirstName,
            user.LastName,
            user.Email!,
            user.Bio,
            roles.ToList(),
            user.CreatedAt);
    }

    private async Task<LearningProfileSection<WishlistCourseAiDto>> BuildWishlistAsync(Guid userId, CancellationToken ct)
    {
        var total = await wishlistRepository.CountAsync(userId, ct);
        var items = total == 0
            ? []
            : await wishlistRepository.GetPagedAsync(userId, 0, Cap, ct);

        var dtos = items
            .Where(w => w.Course is not null)
            .Select(w => new WishlistCourseAiDto(
                w.CourseId,
                w.Course!.Title,
                w.Course.Price,
                w.Course.Price == 0m,
                w.CreatedAt))
            .ToList();

        return new LearningProfileSection<WishlistCourseAiDto>(total, total > dtos.Count, dtos);
    }

    private async Task<AchievementsSummaryDto> BuildAchievementsAsync(Guid userId, CancellationToken ct)
    {
        var unlocked = await achievementRepository.ListAsync(
            new UserAchievementsByUserSpecification(userId), ct);

        var progress = await achievementProgressRepository.GetAsync(userId, ct);

        return new AchievementsSummaryDto(
            unlocked.Count,
            unlocked.Select(ua => ua.Code).ToList(),
            progress?.LessonsCompleted ?? 0,
            progress?.CoursesCompleted ?? 0,
            progress?.DistinctCategoriesCompleted ?? 0,
            progress?.ProfileCompleted ?? false);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveCategoryNamesAsync(
        IEnumerable<Guid> categoryIds,
        CancellationToken ct)
    {
        var ids = categoryIds.Distinct().ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var categories = await categoryRepository.ListAsync(new CategoriesByIdsSpecification(ids), ct);
        return categories.ToDictionary(c => c.Id, c => c.Name);
    }

    private static InProgressCourseDto ToInProgressDto(
        Enrollment enrollment,
        IReadOnlyDictionary<Guid, CourseProgressCounts> counts,
        IReadOnlyDictionary<Guid, string> categoryNames)
    {
        var progress = counts.GetValueOrDefault(enrollment.CourseId, new CourseProgressCounts(0, 0));

        var percent = progress.TotalLessons == 0
            ? 0
            : (int)Math.Round(progress.CompletedLessons * 100.0 / progress.TotalLessons);

        return new InProgressCourseDto(
            enrollment.CourseId,
            enrollment.Course!.Title,
            CategoryNameOf(categoryNames, enrollment.Course.CategoryId),
            progress.CompletedLessons,
            progress.TotalLessons,
            percent,
            enrollment.EnrolledAt);
    }

    private static string CategoryNameOf(IReadOnlyDictionary<Guid, string> names, Guid categoryId) =>
        names.GetValueOrDefault(categoryId, "Unknown");
}
