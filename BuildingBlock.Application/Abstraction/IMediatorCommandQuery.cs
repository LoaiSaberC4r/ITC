using MediatR;

namespace BuildingBlock.Application.Abstraction
{
    public interface ICommand<out TResponse> : IRequest<TResponse>
    { }

    public interface ICommand : IRequest<Unit>
    { }

    public interface IQuery<out TResponse> : IRequest<TResponse>
    { }

    // ===== Handlers =====
    public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    { }

    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Unit>
        where TCommand : ICommand
    { }

    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    { }
}