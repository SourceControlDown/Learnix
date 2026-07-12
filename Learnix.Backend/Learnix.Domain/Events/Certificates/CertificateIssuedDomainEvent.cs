using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Certificates;

public sealed record CertificateIssuedDomainEvent(
    Guid CertificateId,
    Guid StudentId,
    Guid CourseId,
    string CourseTitle) : DomainEvent;
