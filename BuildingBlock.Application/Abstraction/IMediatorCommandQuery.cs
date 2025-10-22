using BuildingBlock.Domain.Results;
using MediatR;

namespace BuildingBlock.Application.Abstraction
{
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    { }

    // Command بدون قيمة (نجاح/فشل فقط)
    public interface ICommand : IRequest<Result>
    { }

    // Query بيرجع قيمة
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    { }

    // ===== Handlers =====
    // Handler لـ Command بيرجع قيمة
    public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
        where TCommand : ICommand<TResponse>
    { }

    // Handler لـ Command بدون قيمة
    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
        where TCommand : ICommand
    { }

    // Handler لـ Query
    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
        where TQuery : IQuery<TResponse>
    { }
}