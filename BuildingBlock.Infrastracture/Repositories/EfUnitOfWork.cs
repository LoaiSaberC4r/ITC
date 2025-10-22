using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Data;

namespace BuildingBlock.Infrastracture.Repositories
{
    public class EfUnitOfWork : IUnitOfWork

    {
        private readonly DbContext _context;
        private readonly ConcurrentDictionary<Type, object> _repos = new();

        public EfUnitOfWork(IDbContextProvider provider) => _context = provider.Context;

        // ------------ Repositories ------------
        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            var type = typeof(TEntity);
            if (_repos.TryGetValue(type, out var repo))
                return (IGenericRepository<TEntity>)repo;

            var closedRepoType = typeof(EfGenericRepository<>).MakeGenericType(typeof(TEntity));
            var instance = (IGenericRepository<TEntity>)ActivatorUtilities.CreateInstance(
                _context.GetService<IServiceProvider>(),
                closedRepoType,
                _context
            );

            _repos[type] = instance;
            return instance;
        }

        // ------------ Save ------------
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);

        // ------------ Transactions ------------
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => _context.Database.BeginTransactionAsync(cancellationToken);

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
            => _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => _context.Database.CommitTransactionAsync(cancellationToken);

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => _context.Database.RollbackTransactionAsync(cancellationToken);

        // ------------ Raw SQL (محايد) ------------
        // 1) كـ Entities (لازم T تكون مكوّنة كـ Entity أو Keyless)
        public async Task<T?> SqlQueryRawSingleAsync<T>(string sql, params object[] parameters) where T : class
        {
            // لو T متسجّلة كـ Entity/Keyless:
            return await _context.Set<T>().FromSqlRaw(sql, parameters).AsNoTracking().FirstOrDefaultAsync();
            // بديل EF8/EF9 للـ DTOs من غير DbSet:
            // return await _context.Database.SqlQueryRaw<T>(sql, parameters).AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<List<T>> SqlQueryRawAsync<T>(string sql, params object[] parameters) where T : class
        {
            return await _context.Set<T>().FromSqlRaw(sql, parameters).AsNoTracking().ToListAsync();
            // أو: return await _context.Database.SqlQueryRaw<T>(sql, parameters).AsNoTracking().ToListAsync();
        }

        public Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
            => _context.Database.ExecuteSqlRawAsync(sql, parameters);

        // ================== Specification: Entities ==================
        public (IQueryable<TEntity> data, int count) GetWithSpec<TEntity>(Specification<TEntity> spec) where TEntity : class
            => Repository<TEntity>().GetWithSpec(spec);

        public Task<List<TEntity>> ListWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class
            => Repository<TEntity>().ListWithSpecAsync(spec, ct);

        public Task<TEntity?> FirstOrDefaultWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class
            => Repository<TEntity>().FirstOrDefaultWithSpecAsync(spec, ct);

        public Task<int> CountWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class
            => Repository<TEntity>().CountWithSpecAsync(spec, ct);

        public Task<bool> AnyWithSpecAsync<TEntity>(Specification<TEntity> spec, CancellationToken ct = default) where TEntity : class
            => Repository<TEntity>().AnyWithSpecAsync(spec, ct);

        // ================== Specification: Projections ==================
        public (IQueryable<TOut> data, int count) GetWithSpec<TEntity, TOut>(Specification<TEntity, TOut> spec) where TEntity : class
            => Repository<TEntity>().GetWithSpec<TOut>(spec);

        public Task<List<TOut>> ListWithSpecAsync<TEntity, TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default) where TEntity : class
            => Repository<TEntity>().ListWithSpecAsync<TOut>(spec, ct);

        public Task<TOut?> FirstOrDefaultWithSpecAsync<TEntity, TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default) where TEntity : class
            => Repository<TEntity>().FirstOrDefaultWithSpecAsync<TOut>(spec, ct);

        // ------------ Dispose ------------
        public async ValueTask DisposeAsync() => await _context.DisposeAsync();
    }
}