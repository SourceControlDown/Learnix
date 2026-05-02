namespace Learnix.Infrastructure.Outbox.Payloads;

public sealed record SendUserUnbannedEmailPayload(string ToEmail, string FirstName);
