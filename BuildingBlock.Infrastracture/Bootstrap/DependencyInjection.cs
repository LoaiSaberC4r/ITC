using BuildingBlock.Application.Repositories;
using BuildingBlock.Infrastracture.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlock.Infrastracture.Bootstrap
{
    public static class DependencyInjection
    {
        // TContext: DbContext الخاص بالخدمة (TrafficLightDb, HealthCareDb, ...)
        public static IServiceCollection AddEfInfrastructure<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            // IGenericRepository<TEntity>  -> EfGenericRepository<TEntity, TContext>
            services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<,>));

            // IUnitOfWork -> EfUnitOfWork<TContext>
            services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();

            return services;
        }
    }
}

//builder.Services.AddEfInfrastructure<ApplicationDbContext>();