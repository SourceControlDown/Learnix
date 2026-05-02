namespace Learnix.Infrastructure.Outbox.Payloads;

public sealed record SendUserRoleChangedEmailPayload(string ToEmail, string FirstName, string Role, bool Assigned);
