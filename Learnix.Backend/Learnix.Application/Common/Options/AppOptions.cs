namespace Learnix.Application.Common.Options;

public class AppOptions
{
    // Required for links generation in emails: {ClientBaseUrl}/verify-email?userId=...&token=...
    public string ClientBaseUrl { get; init; } = null!;  // e.g. "http://localhost:5173"
}
