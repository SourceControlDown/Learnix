using FluentResults;
using Learnix.Application.AiChat.Abstractions.Models;
using MediatR;

namespace Learnix.Application.AiChat.Commands.ClearChatSession;

public sealed record ClearChatSessionCommand(ChatScope Scope) : IRequest<Result>;
