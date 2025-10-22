using BuildingBlock.Application.Abstraction;

namespace ITC.Application.Features.TestManagement.Query.GetTest
{
    public sealed record GetTestQuery : ICacheableQuery<IReadOnlyList<GetTestResponse>>
    {
        public string? CacheKey => $"users";
        public TimeSpan? Ttl => TimeSpan.FromMinutes(5);
        public IEnumerable<string> Tags => new[] { $"user" };
    }
}