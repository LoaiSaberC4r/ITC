using BuildingBlock.Application.Time;
using BuildingBlock.Domain.Primitive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BuildingBlock.Infrastracture.Interceptors
{
    /// <summary>
    /// Generic interceptor to stamp CreatedOnUtc / ModifiedOnUtc for any DbContext
    /// where tracked entities implement IAuditableEntity.
    /// Place in BuildingBlock and register via DI once per service.
    /// </summary>
    public sealed class AuditableEntitiesInterceptor : SaveChangesInterceptor
    {
        private readonly ILogger<AuditableEntitiesInterceptor> _logger;
        private readonly IDateTimeProvider _clock;

        // خصائص نتجاهلها عند تحديد "هل حصل تعديل حقيقي"
        private static readonly HashSet<string> _ignoredProps =
            new(StringComparer.Ordinal)
            {
            nameof(IAuditableEntity.CreatedOnUtc),
            nameof(IAuditableEntity.ModifiedOnUtc)
                // أضف هنا أي حقول تتغير تلقائيًا لا تعتبر "تعديلًا منطقيًا"
                // مثل: RowVersion, ConcurrencyStamp, UpdatedBy ...إلخ
            };

        public AuditableEntitiesInterceptor(
            ILogger<AuditableEntitiesInterceptor> logger,
            IDateTimeProvider clock)
        {
            _logger = logger;
            _clock = clock;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => HandleChangesAsync(eventData, result, cancellationToken);

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
            => HandleChangesAsync(eventData, result).GetAwaiter().GetResult();

        private async ValueTask<InterceptionResult<int>> HandleChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var db = eventData.Context;
            if (db is null)
            {
                _logger.LogWarning("DbContext is null during SavingChanges.");
                return result;
            }

            var now = _clock.UtcNow;

            var entries = db.ChangeTracker
                .Entries<IAuditableEntity>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified);

            foreach (var entry in entries)
            {
                try
                {
                    if (entry.State == EntityState.Added)
                    {
                        // لا نغيّر CreatedOnUtc لو اليوزر عيّنه يدويًا من قبل (مثلاً في seeding)
                        if (entry.Property(x => x.CreatedOnUtc).CurrentValue == default)
                            entry.Property(x => x.CreatedOnUtc).CurrentValue = now;

                        // Added لا نلمس ModifiedOnUtc
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        if (HasMeaningfulChanges(entry))
                        {
                            entry.Property(x => x.ModifiedOnUtc).CurrentValue = now;
                        }
                        else
                        {
                            // لو التغيير كان بس على خواص مهملة (أو لا شيء فعلاً) نلغي التعديل
                            // بحيث ما نكتبش ModifiedOnUtc على الفاضي
                            if (!entry.Properties.Any(p => p.IsModified))
                            {
                                entry.State = EntityState.Unchanged;
                            }
                        }

                        // تأكيد: عدم السماح بتعديل CreatedOnUtc في التعديل
                        if (entry.Property(x => x.CreatedOnUtc).IsModified)
                        {
                            entry.Property(x => x.CreatedOnUtc).IsModified = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auditing failed for entity {EntityType}", entry.Entity.GetType().Name);
                }
            }

            return result;
        }

        private static bool HasMeaningfulChanges(EntityEntry<IAuditableEntity> entry)
        {
            // أي خاصية Modified ليست ضمن قائمة التجاهل تعتبر تغييرًا حقيقيًا
            return entry.Properties.Any(p => p.IsModified && !_ignoredProps.Contains(p.Metadata.Name));
        }
    }
}