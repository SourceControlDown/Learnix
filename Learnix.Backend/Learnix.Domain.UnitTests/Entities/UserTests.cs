using Learnix.Domain.Entities;
using Learnix.Domain.Events.User;

namespace Learnix.Domain.UnitTests.Entities;

public class UserTests
{
    private const string Avatar = "avatars/users/a.jpg";

    private static User NewUser() => new("test@example.com", "John", "Doe");

    [Fact]
    public void Ban_ShouldSetLockoutAndRaiseEvent()
    {
        // Arrange
        var user = NewUser();

        // Act
        user.Ban();

        // Assert
        user.LockoutEnabled.Should().BeTrue();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
        user.DomainEvents.Should().ContainSingle(e => e is UserBannedDomainEvent);
    }

    [Fact]
    public void Unban_ShouldClearLockoutAndRaiseEvent()
    {
        // Arrange
        var user = NewUser();
        user.Ban();
        user.ClearDomainEvents();

        // Act
        user.Unban();

        // Assert
        user.LockoutEnd.Should().BeNull();
        user.DomainEvents.Should().ContainSingle(e => e is UserUnbannedDomainEvent);
    }

    [Fact]
    public void ClaimViaGoogle_ShouldWipePasswordAndConfirmEmail()
    {
        // Arrange
        var user = NewUser();
        user.PasswordHash = "some-old-hash";
        user.EmailConfirmed = false;

        // Act
        user.ClaimViaGoogle("google-123");

        // Assert — the pre-existing password may have been set by someone who never proved ownership
        user.GoogleId.Should().Be("google-123");
        user.PasswordHash.Should().BeNull();
        user.EmailConfirmed.Should().BeTrue();
    }

    // Avatar
    // ======
    [Fact]
    public void SetAvatar_WhenNoPreviousAvatar_ShouldOnlyRaiseTheSetEvent()
    {
        // Arrange
        var user = NewUser();

        // Act
        user.SetAvatar(Avatar);

        // Assert
        user.AvatarBlobPath.Should().Be(Avatar);
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserAvatarSetDomainEvent>();
    }

    [Fact]
    public void SetAvatar_WhenReplacingExisting_ShouldReleaseTheOldBlob()
    {
        // Arrange
        var user = NewUser();
        user.SetAvatar("avatars/users/old.jpg");
        user.ClearDomainEvents();

        // Act
        user.SetAvatar("avatars/users/new.jpg");

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserAvatarRemovedDomainEvent)
            .Which.As<UserAvatarRemovedDomainEvent>().ReleasedBlobPath.Should().Be("avatars/users/old.jpg");
        user.AvatarBlobPath.Should().Be("avatars/users/new.jpg");
    }

    // Profile
    // =======
    [Fact]
    public void UpdateProfile_ShouldRaiseProfileUpdatedEvent()
    {
        // Arrange — the event feeds the profile-completeness achievement
        var user = NewUser();
        user.ClearDomainEvents();

        // Act
        user.UpdateProfile("Jane", "Roe", "bio");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Roe");
        user.Bio.Should().Be("bio");
        user.DomainEvents.Should().ContainSingle(e => e is UserProfileUpdatedDomainEvent);
    }

    // Soft delete
    // ===========
    [Fact]
    public void SoftDelete_ShouldMarkDeletedAndStampTheTime()
    {
        // Arrange
        var user = NewUser();

        // Act
        user.SoftDelete();

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Recover_ShouldClearTheDeletionMarkers()
    {
        // Arrange
        var user = NewUser();
        user.SoftDelete();

        // Act
        user.Recover();

        // Assert
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    // Events raised from the Application layer after Identity operations
    // ==================================================================
    [Fact]
    public void RaiseRoleChanged_ShouldCarryTheRoleAndDirection()
    {
        // Arrange
        var user = NewUser();
        user.ClearDomainEvents();

        // Act
        user.RaiseRoleChanged("Instructor", assigned: true);

        // Assert
        var @event = user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRoleChangedDomainEvent>().Subject;
        @event.Role.Should().Be("Instructor");
        @event.Assigned.Should().BeTrue();
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheQueue()
    {
        // Arrange
        var user = NewUser();
        user.Ban();

        // Act
        user.ClearDomainEvents();

        // Assert
        user.DomainEvents.Should().BeEmpty();
    }
}
