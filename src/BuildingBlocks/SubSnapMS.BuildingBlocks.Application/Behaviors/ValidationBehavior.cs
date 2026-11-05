using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Behaviors;

//ValidationBehavior è il secondo anello della pipeline MediatR.
//Esegue tutti i validator FluentValidation di un command o di una query. Se ci sono errori lancia ValidationException (BuildingBlocks.Web la trasforma in 400 con l'elenco degli errori) e l'handler non viene eseguito.
//Validator eseguiti in sequenza: alcuni possono usare il DbContext, che non è thread-safe.

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();  //raccoglie gli errors dei validators 
        var context = new ValidationContext<TRequest>(request);  //obj che FluentValidation passa alle regole
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);  //aggiunge in un colpo tutti gli errori trovati dai validators alla lista failures
        }
        if (failures.Count > 0)
            throw new ValidationException(failures);
        return await next();
    }
}


