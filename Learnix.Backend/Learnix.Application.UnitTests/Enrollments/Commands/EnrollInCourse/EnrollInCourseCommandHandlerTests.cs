using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Commands.EnrollInCourse;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Enrollments.Commands.EnrollInCourse;

public class EnrollInCourseCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EnrollInCourseCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid InstructorId = Guid.NewGuid();

    public EnrollInCourseCommandHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _sut = new EnrollInCourseCommandHandler(
            _currentUser, _courseRepository, _enrollmentRepository, _wishlistRepository, _unitOfWork);
    }

    private void CourseIs(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private void AlreadyEnrolled(bool value) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(value);

    private Task<FluentResults.Result<EnrollInCourseResponse>> Act(Guid courseId) =>
        _sut.Handle(new EnrollInCourseCommand(courseId), CancellationToken.None);

    [Fact]
    public async Task Enrolling_in_a_free_course_creates_an_active_enrollment_and_clears_the_wishlist_entry()
    {
        var course = PublishedCourse(price: 0m);
        CourseIs(course);
        AlreadyEnrolled(false);

        var result = await Act(course.Id);

        result.IsSuccess.Should().BeTrue();
        course.EnrollmentsCount.Should().Be(1);

        await _enrollmentRepository.Received(1).AddAsync(
            Arg.Is<Enrollment>(e =>
                e.StudentId == StudentId
                && e.PricePaid == 0m
                && e.Status == EnrollmentStatus.Active
                && e.PaymentStatus == PaymentStatus.Completed),
            Arg.Any<CancellationToken>());

        await _wishlistRepository.Received(1).RemoveIfExistsAsync(
            StudentId, course.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The endpoint enrolls, it does not sell. It used to accept a paid course and mark the payment
    /// Completed on the spot, handing out paid access for free and leaving no Payment row behind.
    /// </summary>
    [Fact]
    public async Task A_paid_course_cannot_be_enrolled_into_it_has_to_be_paid_for()
    {
        var course = PublishedCourse(price: 49.99m);
        CourseIs(course);
        AlreadyEnrolled(false);

        var result = await Act(course.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();

        await _enrollmentRepository.DidNotReceiveWithAnyArgs()
            .AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        course.EnrollmentsCount.Should().Be(0);
    }

    [Fact]
    public async Task An_unpublished_course_takes_no_students()
    {
        // Arrange
        var course = Course.Create(InstructorId, Guid.NewGuid(), "Draft", "…", 0m);
        CourseIs(course);

        // Act
        var result = await Act(course.Id);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
    }

    [Fact]
    public async Task An_instructor_cannot_enroll_in_their_own_course()
    {
        // Arrange
        _currentUser.UserId.Returns(InstructorId);
        CourseIs(PublishedCourse(price: 0m));

        // Act
        var result = await Act(Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
    }

    [Fact]
    public async Task Enrolling_twice_is_a_conflict_not_a_second_enrollment()
    {
        // Arrange
        CourseIs(PublishedCourse(price: 0m));
        AlreadyEnrolled(true);

        // Act
        var result = await Act(Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
        await _enrollmentRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task A_course_that_does_not_exist_is_not_found()
    {
        // Arrange
        CourseIs(null);

        // Act
        var result = await Act(Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task An_anonymous_request_never_reaches_the_database()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await Act(Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<AuthenticationError>();
        await _courseRepository.DidNotReceiveWithAnyArgs()
            .FirstOrDefaultAsync(default(ISingleResultSpecification<Course>)!, default);
    }

    private static Course PublishedCourse(decimal price)
    {
        var course = Course.Create(InstructorId, Guid.NewGuid(), "React", "Hooks and more", price);
        course.SetCoverImage("covers/react.webp");

        var section = course.AddSection("Basics");
        var lesson = PostLesson.Create(section.Id, "Intro", "body");
        course.AddLesson(lesson);
        course.ToggleLessonVisibility(lesson, true);

        course.Publish();

        return course;
    }
}
