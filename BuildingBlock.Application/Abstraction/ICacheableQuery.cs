namespace BuildingBlock.Application.Abstraction
{
    public interface ICacheableQuery<TResponse> : IQuery<TResponse>
    {
        /// مفتاح الكاش (اختياري). لو null بنبنيه تلقائياً من نوع الطلب + قيمه
        public string? CacheKey => null;

        /// زمن حياة الكاش (اختياري). لو null = Default (مثلاً 5 دقائق)
        public TimeSpan? Ttl => null;

        /// Tags لحصر علاقات الداتا (مثلاً "users", $"user:{id}")
        public IEnumerable<string> Tags => Enumerable.Empty<string>();
    }
}