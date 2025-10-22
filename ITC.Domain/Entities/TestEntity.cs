using BuildingBlock.Domain.EntitiesHelper;

namespace ITC.Domain.Entities
{
    public class TestEntity : Entity<Guid>
    {
        public string Name { get; private set; } = string.Empty;

        private TestEntity()
        { }

        public static TestEntity Create(string name) => new TestEntity()
        { Name = name ?? throw new ArgumentNullException(nameof(name)) };

        public void SetName(string name)
        {
            Name = name;
        }
    }
}