namespace Learnix.Infrastructure.Outbox.Payloads.Notifications;

internal record NotifyCertificateIssuedPayload(
    Guid UserId,
    Guid CertificateId,
    Guid CourseId,
    string CourseTitle);
