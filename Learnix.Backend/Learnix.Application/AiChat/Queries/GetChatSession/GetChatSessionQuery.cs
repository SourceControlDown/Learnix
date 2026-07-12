using FluentResults;
using Learnix.Application.AiChat.Abstractions.Models;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetChatSession;

public sealed record GetChatSessionQuery(ChatScope Scope) : IRequest<Result<ChatSessionDto>>;
