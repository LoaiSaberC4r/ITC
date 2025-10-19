using BuildingBlock.Domain.Exceptions;
using BuildingBlock.Domain.Results;
using MediatR;

namespace BuildingBlock.Application.Behaviors
{
    public sealed class ExceptionMappingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                // لو TResponse هو Result/Result<T> رجّع Failure بدل ما ترمي، وإلا ارمِ وخلي الميدلوير يعالج
                var tResp = typeof(TResponse);
                if (tResp == typeof(Result))
                    return (TResponse)(object)Result.Fail(ExceptionToErrorMapper.ToError(ex));
                if (tResp.IsGenericType && tResp.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var fail = typeof(Result<>).MakeGenericType(tResp.GetGenericArguments()[0])
                                               .GetMethod("Fail", new[] { typeof(Domain.Results.Error) })!;
                    return (TResponse)fail.Invoke(null, new object[] { ExceptionToErrorMapper.ToError(ex) })!;
                }
                throw; // fallback → هيتم التقاطه في ExceptionHandlingMiddleware العالمي
            }
        }
    }
}