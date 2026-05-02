namespace Learnix.Infrastructure.Outbox.Payloads;

public sealed record SendUserBannedEmailPayload(string ToEmail, string FirstName);
