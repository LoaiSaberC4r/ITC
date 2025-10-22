using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using ITC.Domain.Entities;

namespace ITC.Application.Features.TestManagement.Query.GetTest
{
    public sealed class GetTestQueryHandler : IQueryHandler<GetTestQuery, IReadOnlyList<GetTestResponse>>
    {
        private readonly IGenericRepository<TestEntity> _repository;

        public GetTestQueryHandler(IGenericRepository<TestEntity> repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<GetTestResponse>>> Handle(GetTestQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetTestSpecification();
            var entities = await _repository.ListWithSpecAsync(spec, cancellationToken);
            return Result<IReadOnlyList<GetTestResponse>>.Ok(entities);
        }
    }
}