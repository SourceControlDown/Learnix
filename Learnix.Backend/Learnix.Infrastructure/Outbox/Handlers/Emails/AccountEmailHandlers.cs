using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Infrastructure.Outbox.Payloads.Users;

namespace Learnix.Infrastructure.Outbox.Handlers.Emails;

/// <summary>Emails about the account itself: confirmation, password, suspension, deletion.</summary>
internal sealed class EmailConfirmationHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendEmailConfirmationPayload>
{
    public override string MessageType => OutboxMessageTypes.EmailConfirmation;

    protected override Task HandleAsync(SendEmailConfirmationPayload payload, CancellationToken ct) =>
        emailSender.SendEmailConfirmationAsync(
            payload.ToEmail, payload.FirstName, payload.ConfirmationCode, payload.Language, ct);
}

internal sealed class PasswordResetEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendPasswordResetEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.PasswordResetEmail;

    protected override Task HandleAsync(SendPasswordResetEmailPayload payload, CancellationToken ct) =>
        emailSender.SendPasswordResetAsync(
            payload.ToEmail, payload.FirstName, payload.ResetLink, payload.Language, ct);
}

internal sealed class UserBannedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendUserBannedEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.UserBannedEmail;

    protected override Task HandleAsync(SendUserBannedEmailPayload payload, CancellationToken ct) =>
        emailSender.SendUserBannedAsync(payload.ToEmail, payload.FirstName, payload.Language, ct);
}

internal sealed class UserUnbannedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendUserUnbannedEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.UserUnbannedEmail;

    protected override Task HandleAsync(SendUserUnbannedEmailPayload payload, CancellationToken ct) =>
        emailSender.SendUserUnbannedAsync(payload.ToEmail, payload.FirstName, payload.Language, ct);
}

internal sealed class UserRoleChangedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendUserRoleChangedEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.UserRoleChangedEmail;

    protected override Task HandleAsync(SendUserRoleChangedEmailPayload payload, CancellationToken ct) =>
        emailSender.SendUserRoleChangedAsync(
            payload.ToEmail, payload.FirstName, payload.Role, payload.Assigned, payload.Language, ct);
}

internal sealed class AccountDeletedEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendAccountDeletedEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.AccountDeletedEmail;

    protected override Task HandleAsync(SendAccountDeletedEmailPayload payload, CancellationToken ct) =>
        emailSender.SendAccountDeletedAsync(
            payload.ToEmail, payload.FirstName, payload.PurgeAfterUtc, payload.Language, ct);
}

internal sealed class AccountRecoveredEmailHandler(IEmailSender emailSender)
    : OutboxMessageHandler<SendAccountRecoveredEmailPayload>
{
    public override string MessageType => OutboxMessageTypes.AccountRecoveredEmail;

    protected override Task HandleAsync(SendAccountRecoveredEmailPayload payload, CancellationToken ct) =>
        emailSender.SendAccountRecoveredAsync(payload.ToEmail, payload.FirstName, payload.Language, ct);
}
