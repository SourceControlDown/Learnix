using FluentResults;
using Learnix.Application.Common.Interfaces;
using MediatR;

namespace Learnix.Application.Auth.Commands.ResendConfirmationEmail;

internal sealed class ResendConfirmationEmailCommandHandler(IIdentityService identityService)
    : IRequestHandler<ResendConfirmationEmailCommand, Result>
{
    public Task<Result> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        => identityService.ResendConfirmationEmailAsync(request.Email, cancellationToken);
}
