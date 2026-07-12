using System.Globalization;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Application.Common.Settings;
using Learnix.Infrastructure.Email.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Learnix.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<SmtpSettings> options,
    IOptions<AppSettings> appSettings,
    EmailRenderer renderer,
    IStringLocalizerFactory localizerFactory,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _settings = options.Value;
    private readonly AppSettings _appSettings = appSettings.Value;
    private readonly IStringLocalizer _localizer = localizerFactory.Create(typeof(EmailStrings));

    public async Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationCode, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["EmailConfirmation_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("EmailConfirmation.cshtml", new EmailConfirmationModel
        {
            FirstName = firstName,
            ConfirmationCode = confirmationCode,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["PasswordReset_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("PasswordReset.cshtml", new PasswordResetModel
        {
            FirstName = firstName,
            ResetLink = resetLink,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["InstructorApproved_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("InstructorApproved.cshtml", new InstructorApprovedModel
        {
            FirstName = firstName,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["InstructorRejected_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("InstructorRejected.cshtml", new InstructorRejectedModel
        {
            FirstName = firstName,
            RejectionReason = rejectionReason,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendUserBannedAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["UserBanned_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("UserBanned.cshtml", new UserBannedModel
        {
            FirstName = firstName,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendUserUnbannedAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["UserUnbanned_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("UserUnbanned.cshtml", new UserUnbannedModel
        {
            FirstName = firstName,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendAccountDeletedAsync(string toEmail, string firstName, DateTime purgeAfterUtc, string language = "en", CancellationToken cancellationToken = default)
    {
        var culture = new CultureInfo(language);
        CultureInfo.CurrentUICulture = culture;

        var subject = $"{_localizer["AccountDeleted_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("AccountDeleted.cshtml", new AccountDeletedModel
        {
            FirstName = firstName,
            PurgeDate = purgeAfterUtc.ToString("d MMMM yyyy", culture),
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendAccountRecoveredAsync(string toEmail, string firstName, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["AccountRecovered_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("AccountRecovered.cshtml", new AccountRecoveredModel
        {
            FirstName = firstName,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendUserRoleChangedAsync(string toEmail, string firstName, string role, bool assigned, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["UserRoleChanged_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("UserRoleChanged.cshtml", new UserRoleChangedModel
        {
            FirstName = firstName,
            Role = role,
            Assigned = assigned,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendCourseAdminUnpublishedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["CourseAdminUnpublished_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("CourseAdminUnpublished.cshtml", new CourseAdminActionModel
        {
            InstructorFirstName = instructorFirstName,
            CourseTitle = courseTitle,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    public async Task SendCourseAdminDeletedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken cancellationToken = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["CourseAdminDeleted_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("CourseAdminDeleted.cshtml", new CourseAdminActionModel
        {
            InstructorFirstName = instructorFirstName,
            CourseTitle = courseTitle,
            ClientBaseUrl = _appSettings.ClientBaseUrl,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        var builder = new BodyBuilder { HtmlBody = htmlBody };
        var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Email", "Resources", "logo.png");
        if (System.IO.File.Exists(logoPath))
        {
            var logo = await builder.LinkedResources.AddAsync(logoPath, cancellationToken);
            logo.ContentId = "learnix-logo";
        }
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Email [{Subject}] sent to {Email}", subject, MaskEmail(toEmail));
        }
        // S2139: logging and rethrowing is deliberate. The subject and the masked recipient only exist
        // here — the outbox processor that catches this sees an opaque SMTP exception — and the rethrow
        // is what marks the outbox message for retry.
#pragma warning disable S2139
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email [{Subject}] to {Email}", subject, MaskEmail(toEmail));
            throw;
        }
#pragma warning restore S2139
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "***";
        var parts = email.Split('@');
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
