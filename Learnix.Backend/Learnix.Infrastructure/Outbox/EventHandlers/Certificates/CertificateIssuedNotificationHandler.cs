using Learnix.Domain.Events.Certificates;
using Learnix.Infrastructure.Outbox.Payloads.Notifications;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Certificates;

internal sealed class CertificateIssuedNotificationHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<CertificateIssuedDomainEvent, NotifyCertificateIssuedPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.NotifyCertificateIssued;
    protected override NotifyCertificateIssuedPayload BuildPayload(CertificateIssuedDomainEvent e)
        => new(e.StudentId, e.CertificateId, e.CourseId, e.CourseTitle);
}
