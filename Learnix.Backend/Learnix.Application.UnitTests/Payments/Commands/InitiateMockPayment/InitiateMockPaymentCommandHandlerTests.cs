using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Commands.InitiateMockPayment;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Payments.Commands.InitiateMockPayment;

public class InitiateMockPaymentCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly InitiateMockPaymentCommandHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();

    public InitiateMockPaymentCommandHandlerTests()
    {
        _sut = new InitiateMockPaymentCommandHandler(
            _currentUser, _courseRepository, _enrollmentRepository,
            _paymentRepository, _wishlistRepository, _unitOfWork);

        _currentUser.UserId.Returns(StudentId);
        StubCourse(PublishedCourse(price: 49.99m));
        StubAlreadyEnrolled(false);
    }

    // Guards

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnAuthenticationError()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<AuthenticationError>();
    }

    [Fact]
    public async Task Handle_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        StubCourse(null);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<NotFoundError>();
    }

    [Theory]
    [InlineData(CourseStatus.Draft)]
    [InlineData(CourseStatus.Archived)]
    public async Task Handle_WhenCourseIsNotPublished_ShouldReturnConflict(CourseStatus status)
    {
        // Arrange
        StubCourse(CourseWithStatus(status, price: 49.99m));

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();
    }

    [Fact]
    public async Task Handle_WhenCourseIsFree_ShouldReturnConflictBecauseEnrollmentIsTheRightPath()
    {
        // Arrange
        StubCourse(PublishedCourse(price: 0m));

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStudentIsAlreadyEnrolled_ShouldReturnConflictAndWriteNothing()
    {
        // Arrange
        StubAlreadyEnrolled(true);

        // Act
        var result = await _sut.Handle(Command(), default);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().BeOfType<ConflictError>();

        await _enrollmentRepository.DidNotReceive().AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Happy path

    [Fact]
    public async Task Handle_WhenPaymentSucceeds_ShouldCreateAnEnrollmentWithPaymentAlreadyConfirmed()
    {
        // Arrange — mock payments never go through a gateway, so access is granted immediately
        var course = PublishedCourse(price: 49.99m);
        StubCourse(course);

        Enrollment? captured = null;
        await _enrollmentRepository.AddAsync(Arg.Do<Enrollment>(e => captured = e), Arg.Any<CancellationToken>());
        _enrollmentRepository.ClearReceivedCalls();

        // Act
        var result = await _sut.Handle(Command(course.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.PaymentStatus.Should().Be(PaymentStatus.Completed);
        captured.Status.Should().Be(EnrollmentStatus.Active);
        captured.PricePaid.Should().Be(49.99m);
        result.Value.EnrollmentId.Should().Be(captured.Id);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeds_ShouldChargeTheCoursePriceNotAnyClientSuppliedAmount()
    {
        // Arrange — the command carries no amount; the price must come from the course row
        var course = PublishedCourse(price: 123.45m);
        StubCourse(course);

        Payment? captured = null;
        await _paymentRepository.AddAsync(Arg.Do<Payment>(p => captured = p), Arg.Any<CancellationToken>());
        _paymentRepository.ClearReceivedCalls();

        // Act
        var result = await _sut.Handle(Command(course.Id), default);

        // Assert
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(123.45m);
        captured.UserId.Should().Be(StudentId);
        captured.CourseId.Should().Be(course.Id);
        result.Value.PaymentId.Should().Be(captured.Id);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeds_ShouldIncrementTheDenormalisedEnrollmentsCount()
    {
        // Arrange
        var course = PublishedCourse(price: 49.99m);
        StubCourse(course);
        var before = course.EnrollmentsCount;

        // Act
        await _sut.Handle(Command(course.Id), default);

        // Assert
        course.EnrollmentsCount.Should().Be(before + 1);
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeds_ShouldDropTheCourseFromTheWishlist()
    {
        // Arrange — a purchased course has no business staying on the "want to buy" list
        var course = PublishedCourse(price: 49.99m);
        StubCourse(course);

        // Act
        await _sut.Handle(Command(course.Id), default);

        // Assert
        await _wishlistRepository.Received(1)
            .RemoveIfExistsAsync(StudentId, course.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentSucceeds_ShouldPersistEverythingInOneSaveChanges()
    {
        // Act
        await _sut.Handle(Command(), default);

        // Assert — enrollment, payment and the course counter must land together
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Fixtures

    private static InitiateMockPaymentCommand Command(Guid? courseId = null) =>
        new(courseId ?? Guid.NewGuid());

    private static Course PublishedCourse(decimal price) =>
        CourseWithStatus(CourseStatus.Published, price);

    /// <summary>
    /// Course.Publish() enforces the "at least one visible lesson" invariant, which is irrelevant here.
    /// The status is set directly so the fixture stays about payment, not about course structure.
    /// </summary>
    private static Course CourseWithStatus(CourseStatus status, decimal price)
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "React", "Learn React", price);

        typeof(Course)
            .GetProperty(nameof(Course.Status))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(course, [status]);

        return course;
    }

    private void StubCourse(Course? course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private void StubAlreadyEnrolled(bool enrolled) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(enrolled);
}
