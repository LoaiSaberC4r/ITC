using BuildingBlock.Domain.Enums;
using BuildingBlock.Domain.Specification;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlock.Infrastracture.SpecificationEvaluator
{
    public static class SpecificationEvaluator<TEntity> where TEntity : class
    {
        public static (IQueryable<TEntity> data, int count) GetQuery(
            IQueryable<TEntity> inputQuery,
            Specification<TEntity> spec)
        {
            return BuildCore(inputQuery, spec);
        }

        public static (IQueryable<TOut> data, int count) GetQuery<TOut>(
            IQueryable<TEntity> inputQuery,
            Specification<TEntity, TOut> spec)
        {
            var (entityQuery, count) = BuildCore(inputQuery, spec);

            if (spec.Selector is null)
                throw new InvalidOperationException("Projection selector is not defined.");

            var projected = entityQuery.Select(spec.Selector);
            return (projected, count);
        }

        // ----------------------- Internal -----------------------
        private static (IQueryable<TEntity> query, int count) BuildCore(
            IQueryable<TEntity> inputQuery,
            Specification<TEntity> spec)
        {
            IQueryable<TEntity> query = inputQuery;

            // Ignore global filters
            if (spec.IsGlobalFiltersIgnored)
                query = query.IgnoreQueryFilters();

            // Criteria
            if (spec.Criteria is not null)
                query = query.Where(spec.Criteria);

            // Count AFTER filters, BEFORE paging
            int count = spec.IsTotalCountEnabled ? query.Count() : 0;

            // Tracking
            query = spec.Tracking switch
            {
                TrackingBehavior.NoTracking => query.AsNoTracking(),
                TrackingBehavior.NoTrackingWithIdentityResolution => query.AsNoTrackingWithIdentityResolution(),
                _ => query
            };

            // Strong-typed Include pipelines
            if (spec.IncludePipelines.Count > 0)
            {
                foreach (var pipeline in spec.IncludePipelines)
                    query = pipeline(query);
            }

            // Ordering (desc takes priority if provided)
            if (spec.OrderByDescendingExpressions.Count > 0)
            {
                IOrderedQueryable<TEntity>? ordered = null;
                foreach (var expr in spec.OrderByDescendingExpressions)
                    ordered = ordered is null ? query.OrderByDescending(expr) : ordered.ThenByDescending(expr);
                query = ordered!;
            }
            else if (spec.OrderByExpressions.Count > 0)
            {
                IOrderedQueryable<TEntity>? ordered = null;
                foreach (var expr in spec.OrderByExpressions)
                    ordered = ordered is null ? query.OrderBy(expr) : ordered.ThenBy(expr);
                query = ordered!;
            }

            // Distinct
            if (spec.IsDistinct)
                query = query.Distinct();

            // Paging
            if (spec.IsPagingEnabled)
                query = query.Skip(spec.Skip).Take(spec.Take);

            // Split / Single query toggle
            if (spec.IsSplitQuery)
                query = query.AsSplitQuery();
            else if (spec.IsSingleQuery)
                query = query.AsSingleQuery();

            // Transforms (TagWith, IgnoreAutoIncludes, Temporal, ...)
            if (spec.QueryTransforms.Count > 0)
            {
                foreach (var t in spec.QueryTransforms)
                    query = t(query);
            }

            return (query, count);
        }
    }
}