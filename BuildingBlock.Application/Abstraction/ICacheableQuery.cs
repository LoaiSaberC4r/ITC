namespace BuildingBlock.Application.Abstraction
{
    public interface ICacheableQuery<out TResponse> : IQuery<TResponse>
    {
        /// مفتاح الكاش (اختياري). لو null بنبنيه تلقائياً من نوع الطلب + قيمه
        string? CacheKey => null;

        /// زمن حياة الكاش (اختياري). لو null = Default (مثلاً 5 دقائق)
        TimeSpan? Ttl => null;

        /// Tags لحصر علاقات الداتا (مثلاً "users", $"user:{id}")
        IEnumerable<string> Tags => Enumerable.Empty<string>();
    }
}