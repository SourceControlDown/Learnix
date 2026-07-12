namespace Learnix.Application.Common.Options;

public sealed class GoogleOptions
{
    /// <summary>OAuth Client ID from Google Cloud Console. Used as audience when validating ID tokens.</summary>
    public string ClientId { get; init; } = null!;
}
