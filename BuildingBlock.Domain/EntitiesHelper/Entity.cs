using BuildingBlock.Domain.Primitive;

namespace BuildingBlock.Domain.EntitiesHelper
{
    public abstract class Entity<TKey> : IAuditableEntity
    {
        protected Entity(TKey id) => Id = id;

        protected Entity()
        { }

        public TKey Id { get; init; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? ModifiedOnUtc { get; set; }
    }
}