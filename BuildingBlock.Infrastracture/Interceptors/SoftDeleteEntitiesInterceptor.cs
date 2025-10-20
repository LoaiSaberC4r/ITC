using BuildingBlock.Application.Time;
using BuildingBlock.Domain.Primitive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildingBlock.Infrastracture.Interceptors
{
    public sealed class SoftDeleteEntitiesInterceptor : SaveChangesInterceptor
    {
        private readonly ILogger<SoftDeleteEntitiesInterceptor> _logger;
        private readonly IDateTimeProvider _clock;

        public SoftDeleteEntitiesInterceptor(
            ILogger<SoftDeleteEntitiesInterceptor> logger,
            IDateTimeProvider clock)
        {
            _logger = logger;
            _clock = clock;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => HandleAsync(eventData, result);

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
            => HandleAsync(eventData, result).GetAwaiter().GetResult();

        private async ValueTask<InterceptionResult<int>> HandleAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            var db = eventData.Context;
            if (db is null)
            {
                _logger.LogWarning("DbContext is null during SavingChanges (SoftDelete).");
                return result;
            }

            var now = _clock.UtcNow;

            // 1) أي كيان حالته Deleted => حوّله لـ Modified وطبّق soft delete
            var deletedEntries = db.ChangeTracker
                .Entries<ISoftDeleteEntity>()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in deletedEntries)
            {
                try
                {
                    ApplySoftDelete(entry, now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Soft delete failed for entity {EntityType}", entry.Entity.GetType().Name);
                }
            }

            // 2) أي كيان IsDeleted اتغيّرت (restore أو delete manual)
            var toggledEntries = db.ChangeTracker
                .Entries<ISoftDeleteEntity>()
                .Where(e => e.State == EntityState.Modified &&
                            e.Property(x => x.IsDeleted).IsModified)
                .ToList();

            foreach (var entry in toggledEntries)
            {
                try
                {
                    var isDeleted = entry.Property(x => x.IsDeleted).CurrentValue;
                    if (isDeleted)
                    {
                        // لو حد فعّل IsDeleted يدويًا
                        if (entry.Property(x => x.DeletedOnUtc).CurrentValue is null)
                            entry.Property(x => x.DeletedOnUtc).CurrentValue = now;
                    }
                    else
                    {
                        // Restore
                        entry.Property(x => x.RestoredOnUtc).CurrentValue = now;

                        // عند الاسترجاع من المفضّل تفريغ DeletedOnUtc (اختياري)
                        // entry.Property(x => x.DeletedOnUtc).CurrentValue = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Soft delete toggle failed for entity {EntityType}", entry.Entity.GetType().Name);
                }
            }

            return result;
        }

        private static void ApplySoftDelete(EntityEntry<ISoftDeleteEntity> entry, DateTime now)
        {
            entry.State = EntityState.Modified;
            entry.Property(x => x.IsDeleted).CurrentValue = true;

            // لا تُعيد الكتابة لو كانت موجودة مسبقًا (مثلاً تم حذفها قبل كده)
            if (entry.Property(x => x.DeletedOnUtc).CurrentValue is null)
                entry.Property(x => x.DeletedOnUtc).CurrentValue = now;

            // Owned/child handling: امنع EF من حذف الـ owned تلقائيًا (لو حصل)
            // لو عندك OwnedTypes بيتحوّلوا Deleted تبعًا، رجّعها Unchanged
            foreach (var navigation in entry.References.Where(r => r.TargetEntry is not null))
            {
                var target = navigation.TargetEntry!;
                if (target.State == EntityState.Deleted)
                {
                    target.State = EntityState.Unchanged;
                }
            }
        }
    }
}