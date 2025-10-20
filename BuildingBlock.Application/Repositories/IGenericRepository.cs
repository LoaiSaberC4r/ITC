using BuildingBlock.Domain.Specification;
using System.Linq.Expressions;

namespace BuildingBlock.Application.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        bool HasData();

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task AddRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default);

        void Delete(TEntity entity);

        void DeleteRange(IEnumerable<TEntity> entity);

        void Update(TEntity entity);

        void UpdateRange(IEnumerable<TEntity> entities);

        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<TEntity?> GetByPropertyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

        IReadOnlyList<TEntity> GetAll();

        Task<IEnumerable<TResult>> GetSelectedAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null);

        // ---- Specification (Entity) ----
        (IQueryable<TEntity> data, int count) GetWithSpec(Specification<TEntity> spec);

        Task<List<TEntity>> ListWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default);

        Task<TEntity?> FirstOrDefaultWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default);

        Task<int> CountWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default);

        Task<bool> AnyWithSpecAsync(Specification<TEntity> spec, CancellationToken ct = default);

        // ---- Specification (Projection) ----
        (IQueryable<TOut> data, int count) GetWithSpec<TOut>(Specification<TEntity, TOut> spec);

        Task<List<TOut>> ListWithSpecAsync<TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default);

        Task<TOut?> FirstOrDefaultWithSpecAsync<TOut>(Specification<TEntity, TOut> spec, CancellationToken ct = default);
    }
}