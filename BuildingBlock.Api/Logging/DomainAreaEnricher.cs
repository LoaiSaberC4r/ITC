using Serilog.Core;
using Serilog.Events;

namespace BuildingBlock.Api.Logging
{
    public sealed class DomainAreaEnricher : ILogEventEnricher
    {
        private readonly string _area;

        public DomainAreaEnricher(string area) => _area = area;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
            => logEvent.AddPropertyIfAbsent(factory.CreateProperty("domainArea", _area));
    }
}