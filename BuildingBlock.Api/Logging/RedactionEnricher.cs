using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace BuildingBlock.Api.Logging
{
    public sealed class RedactionEnricher : ILogEventEnricher
    {
        private static readonly Regex Email = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex Phone = new(@"\+?\d[\d\s\-]{7,}\d", RegexOptions.Compiled);
        private const string Mask = "***";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            foreach (var kv in logEvent.Properties)
            {
                if (kv.Value is ScalarValue { Value: string s })
                {
                    var redacted = Redact(s);
                    if (!ReferenceEquals(redacted, s))
                        logEvent.AddOrUpdateProperty(factory.CreateProperty(kv.Key, redacted));
                }
            }
        }

        private static string Redact(string s)
        {
            s = Email.Replace(s, Mask);
            s = Phone.Replace(s, Mask);
            if (s.Contains("Bearer ", StringComparison.OrdinalIgnoreCase))
                s = s.Replace("Bearer ", "Bearer " + Mask, StringComparison.OrdinalIgnoreCase);
            return s;
        }
    }
}