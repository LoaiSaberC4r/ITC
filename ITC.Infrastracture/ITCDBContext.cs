using BuildingBlock.Infrastracture.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ITC.Infrastracture
{
    public class ITCDBContext : DbContext
    {
        public ITCDBContext(DbContextOptions<ITCDBContext> options)
     : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplySoftDeleteQueryFilter();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ITCDBContext).Assembly);
        }
    }
}