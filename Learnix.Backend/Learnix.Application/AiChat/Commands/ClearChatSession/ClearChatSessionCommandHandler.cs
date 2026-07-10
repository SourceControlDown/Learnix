using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Services;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.AiChat.Commands.ClearChatSession;

internal sealed class ClearChatSessionCommandHandler(
    IChatSessionRepository sessionRepository,
    ChatScopeAuthorizer authorizer,
    ICurrentUserService currentUser)
    : IRequestHandler<ClearChatSessionCommand, Result>
{
    public async Task<Result> Handle(ClearChatSessionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var access = await authorizer.EnsureAccessAsync(userId, request.Scope, cancellationToken);
        if (access.IsFailed)
            return access;

        // Deleting one scope leaves the user's other conversations untouched.
        await sessionRepository.DeleteAsync(userId, request.Scope, cancellationToken);

        return Result.Ok();
    }
}
