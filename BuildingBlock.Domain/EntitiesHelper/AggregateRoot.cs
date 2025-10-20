using BuildingBlock.Domain.Primitive;

namespace BuildingBlock.Domain.EntitiesHelper
{
    public class AggregateRoot<TKey> : Entity<TKey>
    {
        private List<IDomainEvent> _domainEvents = new();

        protected AggregateRoot(TKey id)
            : base(id) { }

        protected AggregateRoot()
        { }

        public List<IDomainEvent> GetDomainEvents() => _domainEvents;

        public void ClearDomainEvents() => _domainEvents.Clear();

        protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
            _domainEvents.Add(domainEvent);
    }
}