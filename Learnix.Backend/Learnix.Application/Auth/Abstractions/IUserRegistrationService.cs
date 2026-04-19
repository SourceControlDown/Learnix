using FluentResults;

namespace Learnix.Application.Auth.Abstractions;

public interface IUserRegistrationService
{
    Task<Result<(Guid UserId, string EmailConfirmationToken)>> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken ct = default);

    Task<Result> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken ct = default);

    /// <summary>Always returns Result.Ok — anti-enumeration.</summary>
    Task<Result> ResendConfirmationEmailAsync(
        string email,
        CancellationToken ct = default);
}