namespace Learnix.Infrastructure.Outbox.Payloads.Users;

public sealed record SendAccountRecoveredEmailPayload(string ToEmail, string FirstName, string Language);
