using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Messaging;

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
}