using Learnix.Application.Common.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace Learnix.Infrastructure.Services;

internal sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [Confirmation]: To={ToEmail}, FirstName={FirstName}, Link={Link}",
            toEmail, firstName, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [PasswordReset]: To={ToEmail}, FirstName={FirstName}, Link={Link}",
            toEmail, firstName, resetLink);
        return Task.CompletedTask;
    }

    public Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [InstructorApproved]: To={ToEmail}, FirstName={FirstName}",
            toEmail, firstName);
        return Task.CompletedTask;
    }

    public Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [InstructorRejected]: To={ToEmail}, FirstName={FirstName}, Reason={Reason}",
            toEmail, firstName, rejectionReason ?? "(no reason)");
        return Task.CompletedTask;
    }

    public Task SendUserBannedAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [UserBanned]: To={ToEmail}, FirstName={FirstName}",
            toEmail, firstName);
        return Task.CompletedTask;
    }

    public Task SendUserUnbannedAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [UserUnbanned]: To={ToEmail}, FirstName={FirstName}",
            toEmail, firstName);
        return Task.CompletedTask;
    }

    public Task SendUserRoleChangedAsync(string toEmail, string firstName, string role, bool assigned, CancellationToken ct = default)
    {
        var action = assigned ? "assigned" : "removed";
        logger.LogInformation(
            "EMAIL [UserRoleChanged]: To={ToEmail}, FirstName={FirstName}, Role={Role}, Action={Action}",
            toEmail, firstName, role, action);
        return Task.CompletedTask;
    }

    public Task SendCourseAdminUnpublishedAsync(string toEmail, string instructorFirstName, string courseTitle, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [CourseAdminUnpublished]: To={ToEmail}, Instructor={FirstName}, Course={CourseTitle}",
            toEmail, instructorFirstName, courseTitle);
        return Task.CompletedTask;
    }

    public Task SendCourseAdminDeletedAsync(string toEmail, string instructorFirstName, string courseTitle, CancellationToken ct = default)
    {
        logger.LogInformation(
            "EMAIL [CourseAdminDeleted]: To={ToEmail}, Instructor={FirstName}, Course={CourseTitle}",
            toEmail, instructorFirstName, courseTitle);
        return Task.CompletedTask;
    }
}