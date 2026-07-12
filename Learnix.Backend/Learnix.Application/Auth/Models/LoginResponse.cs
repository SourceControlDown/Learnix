namespace Learnix.Application.Auth.Models;

/// <summary>
/// What every path that ends in an authenticated session returns: Login, Register, GoogleLogin,
/// ConfirmEmail and RefreshToken. Shared by five use cases, so it lives at the feature level rather
/// than inside any one of their folders.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string? AvatarUrl = null);
