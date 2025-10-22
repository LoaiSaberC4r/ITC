using BuildingBlock.Domain.Specification;
using ITC.Domain.Entities;

namespace ITC.Application.Features.TestManagement.Query.GetTest
{
    public class GetTestSpecification : Specification<TestEntity, GetTestResponse>
    {
        public GetTestSpecification()
        {
            AddOrderByDescending(x => x.CreatedOnUtc);
            //Select(x => new GetTestResponse { Name = x.Name });
        }
    }
}