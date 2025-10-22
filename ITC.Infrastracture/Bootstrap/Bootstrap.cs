using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Infrastracture.Bootstrap;
using BuildingBlock.Infrastracture.Interceptors;
using BuildingBlock.Infrastracture.Repositories;
using BuildingBlock.Infrastracture.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ITC.Infrastracture.Bootstrap
{
    public static class Bootstrap
    {
        public static IServiceCollection InfrastructureInjection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbConfig(configuration);
            services.AddScoped<IDbContextProvider, DbContextProvider<ITCDBContext>>();
            services.AddEfInfrastructure();
            services.AddBuildingBlockAuditingAndSoftDelete();
            services.EnsureDatabaseInitializationAndUpToDate();
            services.AddHttpClient();
            services.AddSingleton<ICacheService, MemoryCacheService>();

            return services;
        }

        private static IServiceCollection AddDbConfig(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("Database")!;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A valid database connection string must be provided.");

            services.AddDbContext<ITCDBContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString);
                options.AddInterceptors(sp.GetRequiredService<SoftDeleteEntitiesInterceptor>());
                options.AddInterceptors(sp.GetRequiredService<AuditableEntitiesInterceptor>());
            });

            return services;
        }

        #region Database ifnitialization

        private static void EnsureDatabaseInitializationAndUpToDate(this IServiceCollection services)
        {
            using var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<ITCDBContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<ITCDBContext>>();

            try
            {
                if (!dbContext.Database.CanConnect())
                {
                    dbContext.Database.EnsureCreated();
                    EnsureMigrationHistoryTableExists(dbContext, logger);
                    MarkMigrationsAsApplied(dbContext, logger);
                    logger.LogInformation("Database created, and migrations marked as applied.");
                    return;
                }

                ApplyOrMarkPendingMigrations(dbContext, logger);
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred during database initialization: {ex.Message}");
            }
        }

        private static void EnsureMigrationHistoryTableExists(ITCDBContext dbContext, ILogger<ITCDBContext> logger)
        {
            const string checkTableQuery = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
                               WHERE TABLE_NAME = '__EFMigrationsHistory')
                BEGIN
                    CREATE TABLE __EFMigrationsHistory (
                        MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY,
                        ProductVersion NVARCHAR(32) NOT NULL
                    );
                END";

            dbContext.Database.ExecuteSqlRaw(checkTableQuery);
            logger.LogInformation("Verified or created the __EFMigrationsHistory table.");
        }

        private static void MarkMigrationsAsApplied(ITCDBContext dbContext, ILogger<ITCDBContext> logger)
        {
            var pendingMigrations = dbContext.Database.GetPendingMigrations();

            if (pendingMigrations.Any())
            {
                logger.LogInformation($"Marking migrations as applied: {string.Join(", ", pendingMigrations)}");
                var efCurrenVersion = GetCurrentEfVersion();
                foreach (var migrationId in pendingMigrations)
                {
                    dbContext.Database.ExecuteSqlInterpolated($@"
                            INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES ({migrationId}, {efCurrenVersion})");
                }

                logger.LogInformation("Migrations marked as applied successfully.");
            }
            else
            {
                logger.LogInformation("No migrations to mark as applied.");
            }
        }

        private static string GetCurrentEfVersion()
        {
            var efAssembly = typeof(DbContext).Assembly;
            var version = efAssembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";
        }

        private static void ApplyOrMarkPendingMigrations(ITCDBContext dbContext, ILogger<ITCDBContext> logger)
        {
            var pendingMigrations = dbContext.Database.GetPendingMigrations();

            if (pendingMigrations.Any())
            {
                logger.LogInformation($"Pending migrations detected: {string.Join(", ", pendingMigrations)}");

                dbContext.Database.Migrate();
                logger.LogInformation($"Applied migrations: {string.Join(", ", pendingMigrations)}");
            }
            else
            {
                logger.LogInformation("No pending migrations.");
            }
        }

        #endregion Database ifnitialization
    }
}