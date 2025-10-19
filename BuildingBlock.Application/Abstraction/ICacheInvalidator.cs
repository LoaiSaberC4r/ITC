namespace BuildingBlock.Application.Abstraction
{
    public interface ICacheInvalidator
    {
        /// Tags المطلوب تفريغها بعد نجاح الأمر (مثلاً "users", $"user:{id}")
        IEnumerable<string> Tags { get; }
    }
}