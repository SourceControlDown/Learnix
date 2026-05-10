using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Infrastructure.Email;
using Learnix.Infrastructure.Email.Models;
using Learnix.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Learnix.Infrastructure.Services;

internal sealed class SmtpEmailSender(
    IOptions<SmtpSettings> options,
    EmailRenderer renderer,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _settings = options.Value;

    private const string SubjectConfirmation = "Підтвердіть електронну пошту — Learnix";
    private const string SubjectPasswordReset = "Скидання пароля — Learnix";
    private const string SubjectInstructorApproved = "Заявку схвалено — Learnix";
    private const string SubjectInstructorRejected = "Заявку відхилено — Learnix";
    private const string SubjectUserBanned = "Обліковий запис заблоковано — Learnix";
    private const string SubjectUserUnbanned = "Обліковий запис розблоковано — Learnix";
    private const string SubjectUserRoleChanged = "Зміна ролі — Learnix";
    private const string SubjectCourseUnpublished = "Курс знято з публікації — Learnix";
    private const string SubjectCourseDeleted = "Курс видалено — Learnix";

    public async Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationLink, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("EmailConfirmation.cshtml", new EmailConfirmationModel
        {
            FirstName = firstName,
            ConfirmationLink = confirmationLink
        });
        await SendAsync(toEmail, SubjectConfirmation, html, ct);
    }

    public async Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("PasswordReset.cshtml", new PasswordResetModel
        {
            FirstName = firstName,
            ResetLink = resetLink
        });
        await SendAsync(toEmail, SubjectPasswordReset, html, ct);
    }

    public async Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("InstructorApproved.cshtml", new InstructorApprovedModel
        {
            FirstName = firstName
        });
        await SendAsync(toEmail, SubjectInstructorApproved, html, ct);
    }

    public async Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("InstructorRejected.cshtml", new InstructorRejectedModel
        {
            FirstName = firstName,
            RejectionReason = rejectionReason
        });
        await SendAsync(toEmail, SubjectInstructorRejected, html, ct);
    }

    public async Task SendUserBannedAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("UserBanned.cshtml", new UserBannedModel
        {
            FirstName = firstName
        });
        await SendAsync(toEmail, SubjectUserBanned, html, ct);
    }

    public async Task SendUserUnbannedAsync(string toEmail, string firstName, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("UserUnbanned.cshtml", new UserUnbannedModel
        {
            FirstName = firstName
        });
        await SendAsync(toEmail, SubjectUserUnbanned, html, ct);
    }

    public async Task SendUserRoleChangedAsync(string toEmail, string firstName, string role, bool assigned, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("UserRoleChanged.cshtml", new UserRoleChangedModel
        {
            FirstName = firstName,
            Role = role,
            Assigned = assigned
        });
        await SendAsync(toEmail, SubjectUserRoleChanged, html, ct);
    }

    public async Task SendCourseAdminUnpublishedAsync(string toEmail, string instructorFirstName, string courseTitle, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("CourseAdminUnpublished.cshtml", new CourseAdminActionModel
        {
            InstructorFirstName = instructorFirstName,
            CourseTitle = courseTitle
        });
        await SendAsync(toEmail, SubjectCourseUnpublished, html, ct);
    }

    public async Task SendCourseAdminDeletedAsync(string toEmail, string instructorFirstName, string courseTitle, CancellationToken ct = default)
    {
        var html = await renderer.RenderAsync("CourseAdminDeleted.cshtml", new CourseAdminActionModel
        {
            InstructorFirstName = instructorFirstName,
            CourseTitle = courseTitle
        });
        await SendAsync(toEmail, SubjectCourseDeleted, html, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email [{Subject}] sent to {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email [{Subject}] to {Email}", subject, toEmail);
            throw;
        }
    }
}
