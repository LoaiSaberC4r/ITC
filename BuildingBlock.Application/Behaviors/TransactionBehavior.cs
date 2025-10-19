using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlock.Application.Behaviors
{
    public interface ITransactionalRequest
    { } // علّم بيها الأوامر التي تحتاج معاملة

    public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
#if NET8_0_OR_GREATER
        private readonly DbContext? _db;

        public TransactionBehavior(DbContext? db = null) => _db = db;

#else
    public TransactionBehavior() { }
#endif

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            // نفّذ مباشرة لو مش Command أو مفيش DbContext
            if (request is not ITransactionalRequest || _db is null)
                return await next();

#if NET8_0_OR_GREATER
            // معاملة للأوامر
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var res = await next();
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return res;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
#else
        return await next();
#endif
        }
    }
}