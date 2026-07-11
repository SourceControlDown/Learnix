using System.Text.Json;
using Learnix.API.Hubs;
using Learnix.Application.Common.Abstractions.Hubs;
using Learnix.Application.Notifications.Abstractions;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.API.Services.Notifications;

internal sealed class SignalRNotificationSender(
    INotificationRepository notificationRepository,
    IHubContext<NotificationsHub, INotificationsHubClient> hubContext)
    : INotificationSender
{
    public async Task SendAsync(
        Guid userId,
        NotificationType type,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        // Stored as JSON, pushed as a map — the same facts, in the shape each side reads best.
        var json = parameters is { Count: > 0 } ? JsonSerializer.Serialize(parameters) : null;

        var notification = Notification.Create(userId, type, json);
        await notificationRepository.AddAsync(notification, ct);
        await notificationRepository.TrimToMaxAsync(userId, NotificationConstants.MaxPerUser, ct);

        await hubContext.Clients
            .Group(NotificationsHub.UserGroup(userId.ToString()))
            .NotificationReceived(new NotificationReceivedPayload(
                notification.Id,
                type.ToString(),
                parameters,
                notification.CreatedAt));
    }
}
