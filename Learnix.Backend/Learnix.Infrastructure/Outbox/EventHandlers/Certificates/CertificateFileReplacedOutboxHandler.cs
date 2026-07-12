using Learnix.Domain.Events.Certificates;

namespace Learnix.Infrastructure.Outbox.EventHandlers.Certificates;

internal sealed class CertificateFileReplacedOutboxHandler(OutboxDbContextHolder holder)
    : SimpleOutboxHandler<CertificateFileReplacedDomainEvent, DeleteBlobPayload>(holder)
{
    protected override string MessageType => OutboxMessageTypes.DeleteBlob;
    protected override DeleteBlobPayload BuildPayload(CertificateFileReplacedDomainEvent e)
        => new(e.PreviousFilePath);
}
