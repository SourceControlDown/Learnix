using FluentResults;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetAiChatStatus;

/// <summary>Whether the assistant can answer right now. Reads what the last chat turns learned — never probes.</summary>
public sealed record GetAiChatStatusQuery : IRequest<Result<AiChatStatusResponse>>;
