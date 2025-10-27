using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using ITC.Domain.Entities;

namespace ITC.Application.Features.TestManagement.Command.Add
{
    public sealed class AddTestCommandHandler : ICommandHandler<AddTestCommand>
    {
        private readonly IGenericRepository<TestEntity> _testRepository;

        public AddTestCommandHandler(IGenericRepository<TestEntity> testRepository)
        {
            _testRepository = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
        }

        public async Task<Result> Handle(AddTestCommand request, CancellationToken cancellationToken)
        {
            var entity = TestEntity.Create(request.Name);
            await _testRepository.AddAsync(entity);
            await _testRepository.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}