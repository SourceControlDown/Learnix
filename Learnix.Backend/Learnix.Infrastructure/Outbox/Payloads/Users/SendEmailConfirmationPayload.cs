namespace Learnix.Infrastructure.Outbox.Payloads.Users;

public sealed record SendEmailConfirmationPayload(
    string ToEmail,
    string FirstName,
    string ConfirmationCode,
    string Language
);
