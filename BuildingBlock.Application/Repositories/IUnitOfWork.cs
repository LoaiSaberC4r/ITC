using BuildingBlock.Domain.Specification;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace BuildingBlock.Application.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Repositories
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;

        // Save Changes
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Transactions
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        // Raw SQL (مزود محايد)
        Task<T?> SqlQueryRawSingleAsync<T>(string sql, params object[] parameters) where T : class;

        Task<List<T>> SqlQueryRawAsync<T>(string sql, params object[] parameters) where T : class;

        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);

        // ================== Specification: Entities ==================
        (IQueryable<TEntity> data, int count) GetWithSpec<TEntity>(Specification<TEntity> spec) where TEntity : class;

        Task<List<TEntity>> ListWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class;

        Task<TEntity?> FirstOrDefaultWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class;

        Task<int> CountWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class;

        Task<bool> AnyWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class;

        // ================== Specification: Projections ==================
        (IQueryable<TOut> data, int count) GetWithSpec<TEntity, TOut>(Specification<TEntity, TOut> spec) where TEntity : class;

        Task<List<TOut>> ListWithSpecAsync<TEntity, TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default) where TEntity : class;

        Task<TOut?> FirstOrDefaultWithSpecAsync<TEntity, TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default) where TEntity : class;
    }
}