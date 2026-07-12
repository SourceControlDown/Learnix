using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.Enrollments;

namespace Learnix.Domain.UnitTests.Entities;

public class EnrollmentTests
{
    private static Enrollment Free() => Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), pricePaid: 0m);

    private static Enrollment Paid() => Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), pricePaid: 49.99m);

    private static Enrollment PaidAndConfirmed()
    {
        var enrollment = Paid();
        enrollment.ConfirmPayment();
        return enrollment;
    }

    // Creation
    // ========
    [Fact]
    public void Create_WhenCourseIsFree_ShouldCompletePaymentImmediately()
    {
        // Act
        var enrollment = Free();

        // Assert — a zero price has nothing to pay, so the student is never left in Pending
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Completed);
        enrollment.PricePaid.Should().Be(0m);
        enrollment.CompletedAt.Should().BeNull();
        enrollment.EnrolledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WhenCourseIsPaid_ShouldStartWithPendingPayment()
    {
        // Act
        var enrollment = Paid();

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Pending);
        enrollment.PricePaid.Should().Be(49.99m);
    }

    // Payment transitions
    // ==================
    [Fact]
    public void ConfirmPayment_ShouldMovePendingPaymentToCompleted()
    {
        // Arrange
        var enrollment = Paid();

        // Act
        enrollment.ConfirmPayment();

        // Assert
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void ConfirmPayment_WhenAlreadyCompleted_ShouldBeIdempotent()
    {
        // Arrange
        var enrollment = PaidAndConfirmed();

        // Act
        var act = () => enrollment.ConfirmPayment();

        // Assert — a duplicate payment callback must not blow up
        act.Should().NotThrow();
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void MarkPaymentFailed_WhenPending_ShouldMoveToFailed()
    {
        // Arrange
        var enrollment = Paid();

        // Act
        enrollment.MarkPaymentFailed();

        // Assert
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkPaymentFailed_WhenPaymentAlreadyCompleted_ShouldThrowDomainException()
    {
        // Arrange
        var enrollment = PaidAndConfirmed();

        // Act
        var act = () => enrollment.MarkPaymentFailed();

        // Assert — money already taken cannot be un-taken by a late failure callback
        act.Should().Throw<DomainException>()
            .WithMessage("Completed payment cannot be marked as failed.");
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    // Completion
    // ==========
    [Fact]
    public void MarkCompleted_WhenPaymentIsConfirmed_ShouldCompleteAndRaiseEvent()
    {
        // Arrange
        var enrollment = PaidAndConfirmed();

        // Act
        enrollment.MarkCompleted();

        // Assert
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var @event = enrollment.DomainEvents.OfType<EnrollmentCompletedDomainEvent>()
            .Should().ContainSingle().Subject;
        @event.StudentId.Should().Be(enrollment.StudentId);
        @event.CourseId.Should().Be(enrollment.CourseId);
    }

    [Fact]
    public void MarkCompleted_WhenPaymentIsPending_ShouldThrowDomainException()
    {
        // Arrange
        var enrollment = Paid();

        // Act
        var act = () => enrollment.MarkCompleted();

        // Assert — an unpaid course cannot be completed, which is what gates the certificate
        act.Should().Throw<DomainException>()
            .WithMessage("Enrollment cannot be completed before payment is confirmed.");
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyCompleted_ShouldNotRaiseASecondEvent()
    {
        // Arrange — the certificate and achievement handlers hang off this event; firing twice
        // would issue a second certificate.
        var enrollment = PaidAndConfirmed();
        enrollment.MarkCompleted();
        var firstCompletedAt = enrollment.CompletedAt;

        // Act
        enrollment.MarkCompleted();

        // Assert
        enrollment.DomainEvents.OfType<EnrollmentCompletedDomainEvent>().Should().ContainSingle();
        enrollment.CompletedAt.Should().Be(firstCompletedAt);
    }
}
