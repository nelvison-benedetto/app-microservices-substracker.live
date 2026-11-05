using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Messaging;

//Marcatore comune a tutti i command. Il TransactionBehavior salva solo dopo un command, mai dopo una query

public interface ICommandBase
{
}

//Command senza risultato (es. Logout). Modifica lo stato
public interface ICommand : IRequest, ICommandBase
{
}

//Command con risultato (es. Login → token). Modifica lo stato
public interface ICommand<out TResult> : IRequest<TResult>, ICommandBase
{
}


