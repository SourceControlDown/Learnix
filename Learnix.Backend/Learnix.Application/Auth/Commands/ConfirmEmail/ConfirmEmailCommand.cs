using FluentResults;
using Learnix.Application.Auth.Models;
using MediatR;

namespace Learnix.Application.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Email, string Token) : IRequest<Result<LoginResponse>>;
