using BuildingBlock.Application.Time;

namespace BuildingBlock.Infrastracture.Time
{
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}