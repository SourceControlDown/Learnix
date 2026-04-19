// Learnix.Infrastructure/Identity/UserRegistrationService.cs
using FluentResults;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Learnix.Infrastructure.Identity;

internal sealed class UserRegistrationService(
    UserManager<User> userManager,
    IUnitOfWork unitOfWork)
    : IUserRegistrationService
{
    public async Task<Result<(Guid UserId, string EmailConfirmationToken)>> RegisterAsync(
        string email, 
        string password, 
        string firstName, 
        string lastName,
        CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Result.Fail<(Guid, string)>(new ConflictError("User with this email already exists."));

        var user = new User(email, firstName, lastName);
        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
            return Result.Fail<(Guid, string)>(createResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Student);
        if (!roleResult.Succeeded)
            return Result.Fail<(Guid, string)>(roleResult.Errors.Select(e => (IError)new Error(e.Description)).ToList());

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        user.RaiseUserRegistered(token);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok((user.Id, token));
    }

    public async Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new NotFoundError("User not found."));

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => (IError)new Error(e.Description)).ToList());
    }

    public async Task<Result> ResendConfirmationEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed)
            return Result.Ok();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        user.RaiseUserRegistered(token);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}