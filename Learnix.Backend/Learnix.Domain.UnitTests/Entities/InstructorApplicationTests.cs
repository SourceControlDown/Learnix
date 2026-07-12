using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.InstructorApplications;

namespace Learnix.Domain.UnitTests.Entities;

public class InstructorApplicationTests
{
    private static InstructorApplication Pending()
        => InstructorApplication.Create(Guid.NewGuid(), "I want to teach", "https://portfolio.dev");

    private static InstructorApplication Rejected(string? reason = "Not enough experience")
    {
        var application = Pending();
        application.Reject(Guid.NewGuid(), reason);
        application.ClearDomainEvents();
        return application;
    }

    [Fact]
    public void Create_ShouldStartPendingAndUnreviewed()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var application = InstructorApplication.Create(userId, "I want to teach", "https://portfolio.dev");

        // Assert
        application.UserId.Should().Be(userId);
        application.MotivationText.Should().Be("I want to teach");
        application.PortfolioUrl.Should().Be("https://portfolio.dev");
        application.Status.Should().Be(ApplicationStatus.Pending);
        application.ReviewedByAdminId.Should().BeNull();
        application.ReviewedAt.Should().BeNull();
        application.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Approve_ShouldRecordTheReviewerAndRaiseEvent()
    {
        // Arrange — the event is what upgrades the user's role, so it must carry the user id
        var application = Pending();
        var adminId = Guid.NewGuid();

        // Act
        application.Approve(adminId);

        // Assert
        application.Status.Should().Be(ApplicationStatus.Approved);
        application.ReviewedByAdminId.Should().Be(adminId);
        application.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var @event = application.DomainEvents.OfType<InstructorApplicationApprovedDomainEvent>()
            .Should().ContainSingle().Subject;
        @event.ApplicationId.Should().Be(application.Id);
        @event.UserId.Should().Be(application.UserId);
    }

    [Fact]
    public void Reject_ShouldRecordTheReasonAndRaiseEvent()
    {
        // Arrange
        var application = Pending();
        var adminId = Guid.NewGuid();

        // Act
        application.Reject(adminId, "Not enough experience");

        // Assert
        application.Status.Should().Be(ApplicationStatus.Rejected);
        application.ReviewedByAdminId.Should().Be(adminId);
        application.RejectionReason.Should().Be("Not enough experience");

        var @event = application.DomainEvents.OfType<InstructorApplicationRejectedDomainEvent>()
            .Should().ContainSingle().Subject;
        @event.UserId.Should().Be(application.UserId);
        @event.RejectionReason.Should().Be("Not enough experience");
    }

    [Fact]
    public void Reject_ShouldAllowAnOmittedReason()
    {
        // Act — the reason is optional in the admin UI
        var application = Pending();
        application.Reject(Guid.NewGuid(), reason: null);

        // Assert
        application.Status.Should().Be(ApplicationStatus.Rejected);
        application.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Resubmit_ShouldReturnToPendingAndWipeThePreviousReview()
    {
        // Arrange — a rejected applicant may try again; none of the old verdict may linger
        var application = Rejected();

        // Act
        application.Resubmit("I have since shipped three courses", "https://portfolio.dev/v2");

        // Assert
        application.Status.Should().Be(ApplicationStatus.Pending);
        application.MotivationText.Should().Be("I have since shipped three courses");
        application.PortfolioUrl.Should().Be("https://portfolio.dev/v2");
        application.RejectionReason.Should().BeNull();
        application.ReviewedByAdminId.Should().BeNull();
        application.ReviewedAt.Should().BeNull();
    }

    [Fact]
    public void Resubmit_ShouldAllowDroppingThePortfolioUrl()
    {
        // Arrange
        var application = Rejected();

        // Act
        application.Resubmit("Reworked motivation", portfolioUrl: null);

        // Assert
        application.PortfolioUrl.Should().BeNull();
    }
}
