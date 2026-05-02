using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetActiveChatSession;

internal sealed class GetActiveChatSessionQueryHandler(IChatSessionRepository sessionRepository)
    : IRequestHandler<GetActiveChatSessionQuery, Result<ChatSessionDto>>
{
    public async Task<Result<ChatSessionDto>> Handle(
        GetActiveChatSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        if (session is null)
            return Result.Ok(new ChatSessionDto(string.Empty, []));

        var messages = session.Messages
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.SentAt))
            .ToList();

        return Result.Ok(new ChatSessionDto(session.Id, messages));
    }
}
