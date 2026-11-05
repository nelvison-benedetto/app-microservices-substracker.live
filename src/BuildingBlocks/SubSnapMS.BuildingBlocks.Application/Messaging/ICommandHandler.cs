using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Messaging;

//Handler di un command senza risultato. Non chiama mai SaveChanges
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

//Handler di un command con risultato. Non chiama mai SaveChanges
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
}