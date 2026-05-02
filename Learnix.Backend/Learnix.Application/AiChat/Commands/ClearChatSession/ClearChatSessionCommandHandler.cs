using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using MediatR;

namespace Learnix.Application.AiChat.Commands.ClearChatSession;

internal sealed class ClearChatSessionCommandHandler(IChatSessionRepository sessionRepository)
    : IRequestHandler<ClearChatSessionCommand, Result>
{
    public async Task<Result> Handle(ClearChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        if (session is not null)
            await sessionRepository.CloseSessionAsync(session.Id, cancellationToken);

        return Result.Ok();
    }
}
