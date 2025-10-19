using MediatR;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace BuildingBlock.Application.Behaviors
{
    public sealed class ResilienceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private static readonly AsyncTimeoutPolicy Timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(3));

        private static readonly AsyncRetryPolicy Retry = Policy
            .Handle<TimeoutRejectedException>()
            .Or<HttpRequestException>()
#if NET8_0_OR_GREATER
            .Or<Microsoft.EntityFrameworkCore.DbUpdateException>()
#endif
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt));

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            return await Retry.ExecuteAsync(async () =>
                await Timeout.ExecuteAsync(async ct2 => await next(), ct));
        }
    }