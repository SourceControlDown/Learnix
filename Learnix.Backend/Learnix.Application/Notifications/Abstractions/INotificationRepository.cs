using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Notifications.Abstractions;

public interface INotificationRepository : IRepositoryBase<Notification>
{
    Task TrimToMaxAsync(Guid userId, int maxCount, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllReadByTypeAsync(Guid userId, Learnix.Domain.Enums.NotificationType type, CancellationToken cancellationToken = default);
}
