using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Certificates;

public sealed record CertificateFileReplacedDomainEvent(
    Guid CertificateId,
    string PreviousFilePath) : DomainEvent;
