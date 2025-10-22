using BuildingBlock.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlock.Infrastracture.Repositories
{
    public sealed class DbContextProvider<TContext> : IDbContextProvider
       where TContext : DbContext
    {
        public DbContext Context { get; }

        public DbContextProvider(TContext context)
        {
            Context = context;
        }
    }
}