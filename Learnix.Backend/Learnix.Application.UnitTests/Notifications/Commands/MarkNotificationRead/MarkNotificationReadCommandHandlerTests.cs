using Ardalis.Specification;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Application.Notifications.Commands.MarkNotificationRead;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MarkNotificationReadCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid NotificationId = Guid.NewGuid();

    public MarkNotificationReadCommandHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _sut = new MarkNotificationReadCommandHandler(_currentUser, _notificationRepository, _unitOfWork);
    }

    private void NotificationIs(Notification? notification) =>
        _notificationRepository
            .FirstOrDefaultAsync(
                Arg.Any<ISingleResultSpecification<Notification>>(), Arg.Any<CancellationToken>())
            .Returns(notification);

    private Task<FluentResults.Result> Act() =>
        _sut.Handle(new MarkNotificationReadCommand(NotificationId), CancellationToken.None);

    [Fact]
    public async Task Your_own_notification_is_marked_read()
    {
        // Arrange
        var notification = Notification.Create(UserId, NotificationType.CertificateReady);
        NotificationIs(notification);

        // Act
        var result = await Act();

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The lookup is scoped by owner, so somebody else's notification simply is not there. This is what
    /// keeps a guessed id from being a way to touch — or confirm the existence of — another user's row.
    /// </summary>
    [Fact]
    public async Task Somebody_elses_notification_is_not_found_rather_than_forbidden()
    {
        // Arrange
        NotificationIs(null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task An_anonymous_request_never_reaches_the_database()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act
        var result = await Act();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<AuthenticationError>();
        await _notificationRepository.DidNotReceiveWithAnyArgs()
            .FirstOrDefaultAsync(default(ISingleResultSpecification<Notification>)!, default);
    }
}
