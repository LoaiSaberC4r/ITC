using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Specification;
using BuildingBlock.Infrastracture.SpecificationEvaluator;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BuildingBlock.Infrastracture.Repositories
{
    internal class EfGenericRepository<TEntity, TContext> : IGenericRepository<TEntity>
        where TEntity : class
        where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly DbSet<TEntity> _set;

        public EfGenericRepository(TContext context)
        {
            _context = context;
            _set = context.Set<TEntity>();
        }

        public bool HasData() => _set.Any();

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
            => _set.AddAsync(entity, cancellationToken).AsTask();

        public Task AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default)
            => _set.AddRangeAsync(entities, cancellationToken);

        public void Delete(TEntity entity) => _set.Remove(entity);

        public void DeleteRange(IEnumerable<TEntity> entity) => _set.RemoveRange(entity);

        public void Update(TEntity entity) => _set.Update(entity);

        public void UpdateRange(IEnumerable<TEntity> entities) => _set.UpdateRange(entities);

        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();

        public Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();

        public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();

        public Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => _set.FindAsync(new object?[] { id }, cancellationToken).AsTask();

        public Task<TEntity?> GetByPropertyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
            => _set.FirstOrDefaultAsync(predicate, cancellationToken)!;

        public Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            => _set.AnyAsync(filter, cancellationToken);

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken) > 0;

        public IReadOnlyList<TEntity> GetAll() => _set.AsNoTracking().ToList();

        public async Task<IEnumerable<TResult>> GetSelectedAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null)
        {
            IQueryable<TEntity> query = _set.AsQueryable();
            if (filter is not null) query = query.Where(filter);
            return await query.Select(selector).ToListAsync();
        }

        // -------- Specification (Entity) --------
        public (IQueryable<TEntity> data, int count) GetWithSpec(Specification<TEntity> spec)
            => SpecificationEvaluator<TEntity>.GetQuery(_set.AsQueryable(), spec);

        public async Task<List<TEntity>> ListWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default)
        {
            var (query, _) = GetWithSpec(spec);
            return await query.ToListAsync(ct);
        }

        public async Task<TEntity?> FirstOrDefaultWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default)
        {
            var (query, _) = GetWithSpec(spec);
            return await query.FirstOrDefaultAsync(ct);
        }

        public Task<int> CountWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default)
        {
            // الـ Evaluator بيحسب العد قبل الـ paging
            var (_, count) = GetWithSpec(spec);
            return Task.FromResult(count);
        }

        public async Task<bool> AnyWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default)
        {
            var (query, _) = GetWithSpec(spec);
            return await query.AnyAsync(ct);
        }

        // -------- Specification (Projection) --------
        public (IQueryable<TOut> data, int count) GetWithSpec<TOut>(Specification<TEntity, TOut> spec)
            => SpecificationEvaluator<TEntity>.GetQuery<TOut>(_set.AsQueryable(), spec);

        public async Task<List<TOut>> ListWithSpecAsync<TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default)
        {
            var (query, _) = GetWithSpec<TOut>(spec);
            return await query.ToListAsync(ct);
        }

        public async Task<TOut?> FirstOrDefaultWithSpecAsync<TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default)
        {
            var (query, _) = GetWithSpec<TOut>(spec);
            return await query.FirstOrDefaultAsync(ct);
        }

        // -------- اختياري: Upsert عام --------
        public async Task UpsertAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        {
            var existing = await _set.FirstOrDefaultAsync(predicate);
            if (existing is not null)
                _context.Entry(existing).CurrentValues.SetValues(entity);
            else
                await _set.AddAsync(entity);
        }
    }
}