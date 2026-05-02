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
}