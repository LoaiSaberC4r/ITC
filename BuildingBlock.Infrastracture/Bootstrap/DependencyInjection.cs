using BuildingBlock.Application.Repositories;
using BuildingBlock.Application.Time;
using BuildingBlock.Infrastracture.Interceptors;
using BuildingBlock.Infrastracture.Repositories;
using BuildingBlock.Infrastracture.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlock.Infrastracture.Bootstrap
{
    public static class DependencyInjection
    {
        // TContext: DbContext الخاص بالخدمة (TrafficLightDb, HealthCareDb, ...)
        public static IServiceCollection AddEfInfrastructure(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            return services;
        }

        public static IServiceCollection AddBuildingBlockAuditing(this IServiceCollection services)
        {
            services.TryAddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
            services.AddScoped<AuditableEntitiesInterceptor>();
            return services;
        }

        public static IServiceCollection AddBuildingBlockSoftDelete(this IServiceCollection services)
        {
            services.TryAddSingleton<IDateTimeProvider, SystemDateTimeProvider>(); // لن يُضاف مرة ثانية
            services.AddScoped<SoftDeleteEntitiesInterceptor>();
            return services;
        }

        public static IServiceCollection AddBuildingBlockAuditingAndSoftDelete(this IServiceCollection services)
        {
            return services
                .AddBuildingBlockAuditing()
                .AddBuildingBlockSoftDelete();
        }
    }
}

//builder.Services.AddEfInfrastructure<ApplicationDbContext>();
//builder.Services.AddBuildingBlockAuditing();
/*
 * builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditableEntitiesInterceptor>();

    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(interceptor);
});
 */