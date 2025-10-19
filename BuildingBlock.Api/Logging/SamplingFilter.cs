using Serilog.Core;
using Serilog.Events;

namespace BuildingBlock.Api.Logging
{
    public sealed class SamplingFilter : ILogEventFilter
    {
        private readonly double _rate;
        private readonly LogEventLevel _maxSampled;
        private readonly ThreadLocal<Random> _rng = new(() => new Random());

        public SamplingFilter(double keepRate, LogEventLevel maxLevelToSample)
        {
            if (keepRate < 0 || keepRate > 1) throw new ArgumentOutOfRangeException(nameof(keepRate));
            _rate = keepRate;
            _maxSampled = maxLevelToSample;
        }

        public bool IsEnabled(LogEvent logEvent)
        {
            if (logEvent.Level > _maxSampled) return true;          // لا نُـسمّع Error/Fatal
            return _rng.Value!.NextDouble() < _rate;                 // عيّنة للباقي
        }
    }
}