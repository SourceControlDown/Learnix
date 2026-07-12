using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Domain.UnitTests.Entities;

public class NotificationTests
{
    [Fact]
    public void Create_ShouldStoreTheFactsAndStartUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act — what happened, and what it happened to. Never a sentence about it (ADR-NOTIF-001).
        var notification = Notification.Create(
            userId, NotificationType.CertificateReady, """{"courseTitle":"React"}""");

        // Assert — the unread count that drives the bell badge depends on this default
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(NotificationType.CertificateReady);
        notification.Parameters.Should().Be("""{"courseTitle":"React"}""");
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldAcceptNoParameters_WhenTheTypeIsTheWholeMessage()
    {
        // Arrange & Act
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.InstructorApproved);

        // Assert
        notification.Parameters.Should().BeNull();
    }

    [Fact]
    public void MarkRead_ShouldBeIdempotent()
    {
        // Arrange
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.CertificateReady);

        // Act
        notification.MarkRead();
        notification.MarkRead();

        // Assert
        notification.IsRead.Should().BeTrue();
    }
}
