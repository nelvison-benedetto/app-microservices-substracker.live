using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Messaging;

/// <summary>
/// Marcatore comune a tutti i command. Il TransactionBehavior salva solo dopo un command,
/// mai dopo una query.
/// </summary>
public interface ICommandBase
{
}

/// <summary>Command senza risultato (es. Logout). Modifica lo stato.</summary>
public interface ICommand : IRequest, ICommandBase
{
}

/// <summary>Command con risultato (es. Login → token). Modifica lo stato.</summary>
public interface ICommand<out TResult> : IRequest<TResult>, ICommandBase
{
}