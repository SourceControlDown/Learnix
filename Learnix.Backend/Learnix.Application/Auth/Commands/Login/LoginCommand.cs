using FluentResults;
using Learnix.Application.Auth.Models;
using MediatR;

namespace Learnix.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
