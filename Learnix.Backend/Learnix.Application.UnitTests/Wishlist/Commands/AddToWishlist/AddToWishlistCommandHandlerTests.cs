using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Application.Wishlist.Commands.AddToWishlist;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.Wishlist.Commands.AddToWishlist;

public class AddToWishlistCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AddToWishlistCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CourseId = Guid.NewGuid();

    public AddToWishlistCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _sut = new AddToWishlistCommandHandler(
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

    private Task<FluentResults.Result> Act() =>
        _sut.Handle(new AddToWishlistCommand(CourseId), CancellationToken.None);

    [Fact]
    public async Task A_published_course_goes_on_the_wishlist()
    {
        CourseIs(PublishedCourse());
        AlreadyEnrolled(false);

        var result = await Act();

        result.IsSuccess.Should().BeTrue();
        await _wishlistRepository.Received(1).AddIfMissingAsync(UserId, CourseId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Wishing for a course you are already taking is not a wish; it is a mistake worth naming.</summary>
    [Fact]
    public async Task A_course_you_are_already_enrolled_in_cannot_be_wished_for()
    {
        CourseIs(PublishedCourse());
        AlreadyEnrolled(true);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ConflictError>();
        await _wishlistRepository.DidNotReceiveWithAnyArgs().AddIfMissingAsync(default, default, default);
    }

    /// <summary>
    /// A draft course is invisible to a student, so it must not become discoverable through the wishlist —
    /// hence NotFound rather than a conflict, which would confirm the course exists.
    /// </summary>
    [Fact]
    public async Task An_unpublished_course_is_reported_as_not_found_not_as_a_refusal()
    {
        CourseIs(Course.Create(Guid.NewGuid(), Guid.NewGuid(), "Draft", "…", 0m));

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        await _wishlistRepository.DidNotReceiveWithAnyArgs().AddIfMissingAsync(default, default, default);
    }

    [Fact]
    public async Task An_anonymous_request_never_reaches_the_database()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await Act();

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<AuthenticationError>();
        await _courseRepository.DidNotReceiveWithAnyArgs()
            .FirstOrDefaultAsync(default(ISingleResultSpecification<Course>)!, default);
    }

    private static Course PublishedCourse()
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "React", "…", 0m);
        course.SetCoverImage("covers/react.webp");

        var section = course.AddSection("Basics");
        var lesson = PostLesson.Create(section.Id, "Intro", "body");
        course.AddLesson(lesson);
        course.ToggleLessonVisibility(lesson, true);

        course.Publish();

        return course;
    }
}
