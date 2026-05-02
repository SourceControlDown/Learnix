using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase, new()
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationTasks = _validators.Select(v => v.ValidateAsync(context, cancellationToken));

        var results = await Task.WhenAll(validationTasks);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var aggregated = new ValidationResult(failures);

        var response = new TResponse();
        response.Reasons.Add(new ValidationError(aggregated));
        return response;
    }
}
