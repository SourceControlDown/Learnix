namespace Learnix.Application.Certificates.Abstractions;

public interface ICertificateNotifier
{
    Task NotifyCertificateIssuedAsync(
        Guid userId,
        Guid certificateId,
        Guid courseId,
        string courseTitle,
        CancellationToken cancellationToken);
}
