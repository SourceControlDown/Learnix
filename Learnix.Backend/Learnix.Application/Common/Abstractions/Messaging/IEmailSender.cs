namespace Learnix.Application.Common.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationCode, string language = "en", CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, string language = "en", CancellationToken cancellationToken = default);
    Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default);
    Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, string language = "en", CancellationToken cancellationToken = default);
    Task SendUserBannedAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default);
    Task SendUserUnbannedAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default);
    Task SendUserRoleChangedAsync(string toEmail, string firstName, string role, bool assigned, string language = "en", CancellationToken cancellationToken = default);

    /// <param name="purgeAfterUtc">
    /// The day the account's personal data is erased on, taken from <c>User.PurgeAfter</c>. Deletion is soft,
    /// and this email is the only place the user is told that — told by when, and told how to undo it.
    /// </param>
    Task SendAccountDeletedAsync(string toEmail, string firstName, DateTime purgeAfterUtc, string language = "en", CancellationToken cancellationToken = default);
    Task SendAccountRecoveredAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default);
    Task SendCourseAdminUnpublishedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken cancellationToken = default);
    Task SendCourseAdminDeletedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken cancellationToken = default);
}
