using Learnix.Application.AiChat.Constants;

namespace Learnix.Application.AiChat.Abstractions.Models;

/// <summary>
/// A provider failure worth remembering past the request that hit it.
/// </summary>
/// <param name="Reason">One of <see cref="AiOutageReasons"/>.</param>
/// <param name="Message">The provider's own words, kept for the log — never shown to the student.</param>
/// <param name="RetryAtUtc">
/// When the provider is worth calling again. Null for an outage with no natural end (a missing or rejected
/// key), which is cleared by a successful turn or by fixing the deployment, not by waiting.
/// </param>
public sealed record AiOutage(string Reason, string Message, DateTime? RetryAtUtc);
