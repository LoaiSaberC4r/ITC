using BuildingBlock.Domain.Enums;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace BuildingBlock.Domain.Specification
{
    public abstract class Specification<TEntity> where TEntity : class
    {
        // ------------------------ Filters & Includes ------------------------
        public Expression<Func<TEntity, bool>> Criteria { get; private set; } = x => true;

        public IReadOnlyList<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>> IncludePipelines
            => _includePipelines.AsReadOnly();

        private readonly List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>> _includePipelines = new();

        // ------------------------ Sorting ------------------------
        public IReadOnlyList<Expression<Func<TEntity, object>>> OrderByExpressions => _orderBy.AsReadOnly();

        private readonly List<Expression<Func<TEntity, object>>> _orderBy = new();

        public IReadOnlyList<Expression<Func<TEntity, object>>> OrderByDescendingExpressions => _orderByDesc.AsReadOnly();
        private readonly List<Expression<Func<TEntity, object>>> _orderByDesc = new();

        // ------------------------ Paging & Count ------------------------
        public int Skip { get; private set; }

        public int Take { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        public bool IsTotalCountEnabled { get; private set; }

        // ------------------------ Misc ------------------------
        public bool IsDistinct { get; private set; }

        public bool IsGlobalFiltersIgnored { get; private set; }
        public TrackingBehavior Tracking { get; private set; } = TrackingBehavior.TrackAll;

        public bool IsSplitQuery { get; private set; }
        public bool IsSingleQuery { get; private set; }

        // Optional transforms (TagWith, IgnoreAutoIncludes, TemporalAsOf, ...)
        public IReadOnlyList<Func<IQueryable<TEntity>, IQueryable<TEntity>>> QueryTransforms
            => _transforms.AsReadOnly();

        private readonly List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> _transforms = new();

        // ------------------------ Fluent API ------------------------
        protected Specification<TEntity> AddCriteria(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            Criteria = Criteria.And(predicate);
            return this;
        }

        /// <summary>
        /// Add a full Include/ThenInclude pipeline.
        /// Example:
        ///   AddInclude(q => q.Include(o => o.Customer).ThenInclude(c => c.Addresses));
        /// </summary>
        protected Specification<TEntity> AddInclude(
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includePipeline)
        {
            if (includePipeline is null) throw new ArgumentNullException(nameof(includePipeline));
            _includePipelines.Add(includePipeline);
            return this;
        }

        /// <summary>
        /// Short DSL helper for simple Include with optional ThenInclude chain.
        /// Example:
        ///   Include(o => o.Customer, inc => inc.ThenInclude(c => c.Addresses));
        /// </summary>
        protected Specification<TEntity> Include<TProperty>(
            Expression<Func<TEntity, TProperty>> navigation,
            Func<IIncludableQueryable<TEntity, TProperty>, IIncludableQueryable<TEntity, object>>? then = null)
        {
            if (navigation is null) throw new ArgumentNullException(nameof(navigation));
            return AddInclude(q =>
            {
                var inc = q.Include(navigation);
                return then is null ? (IIncludableQueryable<TEntity, object>)inc : then(inc);
            });
        }

        protected Specification<TEntity> AddOrderBy(Expression<Func<TEntity, object>> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            _orderBy.Add(keySelector);
            return this;
        }

        protected Specification<TEntity> AddOrderByDescending(Expression<Func<TEntity, object>> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            _orderByDesc.Add(keySelector);
            return this;
        }

        protected Specification<TEntity> ApplyPaging(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
            Skip = (pageNumber - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
            EnableTotalCount();
            return this;
        }

        protected Specification<TEntity> EnableTotalCount()
        {
            IsTotalCountEnabled = true;
            return this;
        }

        protected Specification<TEntity> EnableDistinct()
        {
            IsDistinct = true;
            return this;
        }

        protected Specification<TEntity> IgnoreGlobalFilters()
        {
            IsGlobalFiltersIgnored = true;
            return this;
        }

        protected Specification<TEntity> UseNoTracking()
        {
            Tracking = TrackingBehavior.NoTracking;
            return this;
        }

        protected Specification<TEntity> UseNoTrackingWithIdentityResolution()
        {
            Tracking = TrackingBehavior.NoTrackingWithIdentityResolution;
            return this;
        }

        protected Specification<TEntity> UseTracking()
        {
            Tracking = TrackingBehavior.TrackAll;
            return this;
        }

        protected Specification<TEntity> UseSplitQuery()
        {
            IsSplitQuery = true;
            IsSingleQuery = false;
            return this;
        }

        protected Specification<TEntity> UseSingleQuery()
        {
            IsSingleQuery = true;
            IsSplitQuery = false;
            return this;
        }

        protected Specification<TEntity> AddTransform(Func<IQueryable<TEntity>, IQueryable<TEntity>> transform)
        {
            if (transform is null) throw new ArgumentNullException(nameof(transform));
            _transforms.Add(transform);
            return this;
        }

        protected Specification<TEntity> CombineWith(params Specification<TEntity>[] specs)
        {
            foreach (var s in specs)
            {
                if (s is null) continue;

                Criteria = Criteria.And(s.Criteria);

                foreach (var ip in s.IncludePipelines)
                    _includePipelines.Add(ip);

                foreach (var ob in s.OrderByExpressions)
                    _orderBy.Add(ob);

                foreach (var obd in s.OrderByDescendingExpressions)
                    _orderByDesc.Add(obd);

                if (s.IsPagingEnabled)
                    ApplyPaging((s.Skip / Math.Max(1, s.Take)) + 1, s.Take);

                if (s.IsTotalCountEnabled) EnableTotalCount();
                if (s.IsDistinct) EnableDistinct();
                if (s.IsGlobalFiltersIgnored) IgnoreGlobalFilters();

                if ((int)s.Tracking > (int)Tracking)
                    Tracking = s.Tracking;

                if (s.IsSplitQuery) UseSplitQuery();
                if (s.IsSingleQuery) UseSingleQuery();

                foreach (var t in s.QueryTransforms)
                    _transforms.Add(t);
            }
            return this;
        }
    }

    /// <summary>
    /// Projection specification: تضيف Selector لإخراج TOut مباشرة من قاعدة البيانات.
    /// </summary>
    public abstract class Specification<TEntity, TOut> : Specification<TEntity> where TEntity : class
    {
        public Expression<Func<TEntity, TOut>> Selector { get; private set; } = null!;

        protected Specification<TEntity, TOut> Select(Expression<Func<TEntity, TOut>> selector)
        {
            Selector = selector ?? throw new ArgumentNullException(nameof(selector));
            return this;
        }
    }
}