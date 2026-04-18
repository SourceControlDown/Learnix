using Learnix.Application.Common.Interfaces;
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
}