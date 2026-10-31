using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Esegue tutti i validator FluentValidation del request. Se ci sono errori lancia ValidationException
/// (BuildingBlocks.Web la trasforma in 400 con l'elenco degli errori) e l'handler non viene eseguito.
/// Validator eseguiti in sequenza: alcuni possono usare il DbContext, che non è thread-safe.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        var context = new ValidationContext<TRequest>(request);

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}