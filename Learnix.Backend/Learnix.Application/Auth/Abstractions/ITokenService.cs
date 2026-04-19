using Learnix.Application.Common.Models;

namespace Learnix.Application.Auth.Abstractions;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        string firstName,
        IReadOnlyList<string> roles);

    RefreshTokenResult GenerateRefreshToken();

    string HashRefreshToken(string plainToken);
}
