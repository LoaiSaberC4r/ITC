using BuildingBlock.Domain.Results;
using FluentValidation;
using MediatR;

namespace BuildingBlock.Application.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
 where TResponse : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (!_validators.Any()) return await next();

            var errors = new List<Error>();
            foreach (var v in _validators)
            {
                var res = await v.ValidateAsync(request, ct);
                if (!res.IsValid)
                {
                    errors.AddRange(res.Errors.Select(f =>
                        Error.Validation(
                            code: $"Validation.{(string.IsNullOrWhiteSpace(f.ErrorCode) ? f.PropertyName : f.ErrorCode)}",
                            message: f.ErrorMessage,
                            details: $"field:{f.PropertyName}",
                            source: "FluentValidation")));
                }
            }

            if (errors.Count == 0) return await next();

            // لو TResponse هو Result أو Result<T> نرجّع Failure بطريقة آمنة:
            var tResp = typeof(TResponse);
            if (tResp == typeof(Result))
                return (TResponse)(object)Result.Fail(errors);
            if (tResp.IsGenericType && tResp.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var fail = typeof(Result<>).MakeGenericType(tResp.GetGenericArguments()[0])
                                           .GetMethod("Fail", new[] { typeof(IEnumerable<Error>) })!;
                return (TResponse)fail.Invoke(null, new object[] { errors })!;
            }

            // fallback: ارمي استثناء وخليه يتلمّ في ExceptionMappingBehavior/الميدلوير
            throw new ArgumentException("Validation failed.");
        }
    }
}