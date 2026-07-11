using FluentResults;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Constants;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetAiChatStatus;

internal sealed class GetAiChatStatusQueryHandler(
    IAiChatProvider provider,
    IAiAvailabilityStore availability)
    : IRequestHandler<GetAiChatStatusQuery, Result<AiChatStatusResponse>>
{
    public async Task<Result<AiChatStatusResponse>> Handle(
        GetAiChatStatusQuery request,
        CancellationToken cancellationToken)
    {
        // A deployment with no key is down for good, but the student is told only that it is down.
        if (!provider.IsConfigured)
        {
            return Result.Ok(new AiChatStatusResponse(
                Available: false,
                provider.Name,
                AiOutageReasons.Public(AiOutageReasons.NotConfigured),
                RetryAtUtc: null));
        }

        var outage = await availability.GetOutageAsync(cancellationToken);

        // An outage that has run its course is no outage: the store expires it, and a stale read must not
        // keep the student locked out a moment longer than the provider asked for.
        if (outage is null || outage.RetryAtUtc <= DateTime.UtcNow)
            return Result.Ok(new AiChatStatusResponse(true, provider.Name, null, null));

        return Result.Ok(new AiChatStatusResponse(
            Available: false,
            provider.Name,
            AiOutageReasons.Public(outage.Reason),
            outage.RetryAtUtc));
    }
}
