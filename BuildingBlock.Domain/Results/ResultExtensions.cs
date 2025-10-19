using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace BuildingBlock.Domain.Results
{
    public static class ResultExtensions
    {
        // Ensure (guard)
        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
        => result.IsFailure ? result
           : predicate(result.Value) ? result
           : Result<T>.Fail(error);

        // Map
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
         => result.IsSuccess ? Result<TOut>.Ok(map(result.Value)) : Result<TOut>.Fail(result.Errors);

        // Bind (flatMap)
        public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind)
            => result.IsSuccess ? bind(result.Value) : Result<TOut>.Fail(result.Errors);

        // Tap (side effects)
        public static Result<T> Tap<T>(this Result<T> result, Action<T> effect)
        {
            if (result.IsSuccess) effect(result.Value);
            return result;
        }

        // Match
        public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<ImmutableArray<Error>, TOut> onFailure)
        => result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Errors);

        public static async ValueTask<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, ValueTask<TOut>> mapAsync)
            => result.IsSuccess ? Result<TOut>.Ok(await mapAsync(result.Value)) : Result<TOut>.Fail(result.Errors);

        public static async ValueTask<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, ValueTask<Result<TOut>>> bindAsync)
            => result.IsSuccess ? await bindAsync(result.Value) : Result<TOut>.Fail(result.Errors);

        public static async ValueTask<TOut> MatchAsync<T, TOut>(this Result<T> result, Func<T, ValueTask<TOut>> onSuccess, Func<ImmutableArray<Error>, ValueTask<TOut>> onFailure)
            => result.IsSuccess ? await onSuccess(result.Value) : await onFailure(result.Errors);

        /// <summary>أثر جانبي عند الفشل دون تغيير النتيجة.</summary>
        public static Result<T> TapError<T>(this Result<T> result, Action<IReadOnlyList<Error>> effectOnFailure)
        {
            if (result.IsFailure) effectOnFailure(result.Errors);
            return result;
        }

        /// <summary>Ensure غير متزامن لقواعد تحتاج I/O أو عمليات Async قصيرة.</summary>
        public static async ValueTask<Result<T>> EnsureAsync<T>(
            this Result<T> result,
            Func<T, ValueTask<bool>> predicateAsync,
            Error error)
        {
            if (result.IsFailure) return result;
            return await predicateAsync(result.Value) ? result : Result<T>.Fail(error);
        }

        public static async ValueTask<Result<T>> TapAsync<T>(this ValueTask<Result<T>> task, Action<T> effect)
        {
            var res = await task;
            if (res.IsSuccess) effect(res.Value);
            return res;
        }

        public static async ValueTask<Result<T>> TapErrorAsync<T>(this ValueTask<Result<T>> task, Action<IReadOnlyList<Error>> effectOnFailure)
        {
            var res = await task;
            if (res.IsFailure) effectOnFailure(res.Errors);
            return res;
        }

        /// <summary>مساعد للإلغاء: إن كان ct ملغى يعيد Failure بالخطأ المعطى.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CancellationToken ThrowIfCancellationRequested(this CancellationToken ct, Error cancellationError, out Result failure)
        {
            if (ct.IsCancellationRequested)
            {
                failure = Result.Fail(cancellationError);
            }
            else
            {
                failure = Result.Ok();
            }
            return ct;
        }
    }
}