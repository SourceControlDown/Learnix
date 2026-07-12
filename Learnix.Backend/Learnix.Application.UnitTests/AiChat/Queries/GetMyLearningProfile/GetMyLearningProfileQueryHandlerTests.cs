using Ardalis.Specification;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetMyLearningProfile;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetMyLearningProfile;

public class GetMyLearningProfileQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserRoleService _roleService = Substitute.For<IUserRoleService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ILessonProgressRepository _lessonProgressRepository = Substitute.For<ILessonProgressRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>();
    private readonly IUserAchievementRepository _achievementRepository = Substitute.For<IUserAchievementRepository>();

    private readonly IUserAchievementProgressRepository _achievementProgressRepository =
        Substitute.For<IUserAchievementProgressRepository>();

    private readonly GetMyLearningProfileQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();

    public GetMyLearningProfileQueryHandlerTests()
    {
        _sut = new GetMyLearningProfileQueryHandler(
            _currentUser,
            _userRepository,
            _roleService,
            _enrollmentRepository,
            _lessonProgressRepository,
            _categoryRepository,
            _wishlistRepository,
            _achievementRepository,
            _achievementProgressRepository);

        _currentUser.UserId.Returns(StudentId);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(new GetMyLearningProfileQuery(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task Handle_WhenOnlyWishlistRequested_ShouldNotTouchOtherSections()
    {
        // Arrange
        _wishlistRepository.CountAsync(StudentId, Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await _sut.Handle(
            new GetMyLearningProfileQuery([LearningProfileSections.Wishlist]), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Wishlist.Should().NotBeNull();
        result.Value.Profile.Should().BeNull();
        result.Value.InProgress.Should().BeNull();
        result.Value.Completed.Should().BeNull();
        result.Value.Achievements.Should().BeNull();

        await _enrollmentRepository.DidNotReceive()
            .ListAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCourseHasNoVisibleLessons_ShouldReportZeroPercentInsteadOfDividingByZero()
    {
        // Arrange
        var (enrollment, category) = ActiveEnrollment("Empty course");
        StubEnrollments(enrollment);
        StubCategories(category);
        StubProgress(enrollment.CourseId, completed: 0, total: 0);

        // Act
        var result = await _sut.Handle(
            new GetMyLearningProfileQuery([LearningProfileSections.InProgress]), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.InProgress!.Items.Should().ContainSingle()
            .Which.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCoursePartiallyCompleted_ShouldRoundProgressPercent()
    {
        // Arrange
        var (enrollment, category) = ActiveEnrollment("React");
        StubEnrollments(enrollment);
        StubCategories(category);
        StubProgress(enrollment.CourseId, completed: 7, total: 12);

        // Act
        var result = await _sut.Handle(
            new GetMyLearningProfileQuery([LearningProfileSections.InProgress]), default);

        // Assert
        var course = result.Value.InProgress!.Items.Should().ContainSingle().Subject;
        course.CompletedLessons.Should().Be(7);
        course.TotalLessons.Should().Be(12);
        course.ProgressPercent.Should().Be(58); // 58.33 -> 58
        course.CategoryName.Should().Be(category.Name);
    }

    [Fact]
    public async Task Handle_WhenInProgressCoursesExceedTheCap_ShouldTruncateAndReportTheRealTotal()
    {
        // Arrange
        var overCap = AiChatToolLimits.LearningProfileSectionItems + 3;
        var enrollments = Enumerable.Range(0, overCap)
            .Select(i => ActiveEnrollment($"Course {i}").Enrollment)
            .ToArray();

        StubEnrollments(enrollments);
        StubCategories();
        _lessonProgressRepository
            .GetProgressCountsAsync(StudentId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, CourseProgressCounts>());

        // Act
        var result = await _sut.Handle(
            new GetMyLearningProfileQuery([LearningProfileSections.InProgress]), default);

        // Assert
        var section = result.Value.InProgress!;
        section.Total.Should().Be(overCap);
        section.Truncated.Should().BeTrue();
        section.Items.Should().HaveCount(AiChatToolLimits.LearningProfileSectionItems);

        // Progress is only computed for the courses that are actually returned.
        await _lessonProgressRepository.Received(1).GetProgressCountsAsync(
            StudentId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == AiChatToolLimits.LearningProfileSectionItems),
            Arg.Any<CancellationToken>());
    }

    private static (Enrollment Enrollment, Category Category) ActiveEnrollment(string courseTitle)
    {
        var category = Category.Create("Programming", "programming");
        var course = Course.Create(Guid.NewGuid(), category.Id, courseTitle, "Description", 0m);
        var enrollment = Enrollment.Create(course.Id, StudentId, 0m);

        // Enrollment.Course is populated by EF's Include; there is no domain method to set it.
        typeof(Enrollment)
            .GetProperty(nameof(Enrollment.Course))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(enrollment, [course]);

        return (enrollment, category);
    }

    private void StubEnrollments(params Enrollment[] enrollments) =>
        _enrollmentRepository
            .ListAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrollments.ToList());

    private void StubCategories(params Category[] categories) =>
        _categoryRepository
            .ListAsync(Arg.Any<ISpecification<Category>>(), Arg.Any<CancellationToken>())
            .Returns(categories.ToList());

    private void StubProgress(Guid courseId, int completed, int total) =>
        _lessonProgressRepository
            .GetProgressCountsAsync(StudentId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, CourseProgressCounts>
            {
                [courseId] = new(completed, total)
            });
}
