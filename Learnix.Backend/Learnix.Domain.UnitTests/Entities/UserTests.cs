using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Constants;
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
    public void SoftDelete_ShouldFlagTheAccountAndRaiseEvent()
    {
        // Arrange
        var user = NewUser();

        // Act
        user.SoftDelete();

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();

        // The date the data dies on is written down, not recomputed later: the user is told it in an email,
        // and changing the window afterwards must not move a day somebody was already promised.
        user.PurgeAfter.Should().Be(
            user.DeletedAt!.Value.AddDays(UserConstants.AccountRecoveryWindowDays));

        // The event is what carries the goodbye email — deleting silently would leave the user
        // with no way of knowing the account can still be brought back.
        user.DomainEvents.Should().ContainSingle(e => e is UserDeletedDomainEvent)
            .Which.Should().BeOfType<UserDeletedDomainEvent>()
            .Which.PurgeAfterUtc.Should().Be(user.PurgeAfter!.Value);
    }

    [Fact]
    public void Recover_ShouldUndeleteTheAccountAndRaiseEvent()
    {
        // Arrange
        var user = NewUser();
        user.SoftDelete();
        user.ClearDomainEvents();

        // Act
        user.Recover();

        // Assert
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();

        // A recovered account must not stay on the cleanup service's list.
        user.PurgeAfter.Should().BeNull();
        user.DomainEvents.Should().ContainSingle(e => e is UserRecoveredDomainEvent);
    }

    [Fact]
    public void Anonymize_ShouldStripEveryTraceOfThePersonAndLeaveTheRow()
    {
        // Arrange
        var user = NewUser();
        user.SetAvatar(Avatar);
        user.UpdateProfile("John", "Doe", "I teach C#");
        user.SetGoogleId("google-123");
        user.SoftDelete();
        user.ClearDomainEvents();

        // Act
        user.Anonymize();

        // Assert — nothing personal is left
        user.Email.Should().Be($"deleted-{user.Id:N}@learnix.invalid");
        user.UserName.Should().Be(user.Email);
        user.FirstName.Should().Be(User.AnonymizedFirstName);
        user.LastName.Should().Be(User.AnonymizedLastName);
        user.Bio.Should().BeNull();
        user.GoogleId.Should().BeNull();
        user.PasswordHash.Should().BeNull();
        user.AvatarBlobPath.Should().BeNull();
        user.EmailConfirmed.Should().BeFalse();

        // The account stays deleted, and the purge must not find it a second time.
        user.IsDeleted.Should().BeTrue();
        user.PurgeAfter.Should().BeNull();

        // The avatar file itself is reaped by the outbox message this event enqueues.
        user.DomainEvents.Should().Contain(e => e is UserAvatarRemovedDomainEvent);
        user.DomainEvents.Should().Contain(e => e is UserAnonymizedDomainEvent);
    }

    [Fact]
    public void Anonymize_ShouldRefuseAnAccountThatIsStillLive()
    {
        // Arrange
        var user = NewUser();

        // Act
        var act = user.Anonymize;

        // Assert — anonymizing a live account would erase somebody who never asked to leave.
        act.Should().Throw<DomainException>();
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
