using Microsoft.EntityFrameworkCore;

namespace BuildingBlock.Application.Repositories
{
    public interface IDbContextProvider
    {
        DbContext Context { get; }
    }
}