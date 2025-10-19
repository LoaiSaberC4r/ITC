using System.Collections.Immutable;
using System.Diagnostics;

namespace BuildingBlock.Domain.Results
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public ImmutableArray<Error> Errors { get; }

        private Result(bool isSuccess, ImmutableArray<Error> errors)
        {
            IsSuccess = isSuccess;
            Errors = errors;
        }

        public static Result Ok() => new(true, ImmutableArray<Error>.Empty);

        public static Result Fail(Error error)
            => new(false, ImmutableArray.Create(error));

        public static Result Fail(IEnumerable<Error> errors)
            => new(false, errors?.ToImmutableArray() ?? ImmutableArray<Error>.Empty);

        /// <summary>يرجع أول فشل، أو نجاح إن لم يوجد فشل.</summary>
        public static Result FirstFailureOrOk(params Result[] results)
        {
            foreach (var r in results)
                if (r.IsFailure) return r;
            return Ok();
        }

        /// <summary>Combine (non-generic): إن وُجد فشل نرجع الفشل، وإلا Success.</summary>
        public static Result Combine(params Result[] results)
        {
            foreach (var r in results)
                if (r.IsFailure) return r;
            return Ok();
        }

        public override string ToString() => DebuggerDisplay;

        private string DebuggerDisplay => IsSuccess
            ? "Result: Success"
            : $"Result: Failure ({Errors.Length} errors)";

        [DebuggerStepThrough]
        public void ThrowIfFailure()
        {
            if (IsFailure) throw new InvalidOperationException(
                $"Result is Failure: {string.Join(',', Errors.Select(e => e.Code))}");
        }
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        private readonly T _value;

        public T Value => IsSuccess
            ? _value
            : throw new InvalidOperationException("No value for failure result.");

        public ImmutableArray<Error> Errors { get; }

        private Result(T value)
        {
            IsSuccess = true;
            _value = value;
            Errors = ImmutableArray<Error>.Empty;
        }

        private Result(ImmutableArray<Error> errors)
        {
            IsSuccess = false;
            _value = default!;
            Errors = errors;
        }

        public static Result<T> Ok(T value) => new(value);

        public static Result<T> Fail(Error error) => new(ImmutableArray.Create(error));

        public static Result<T> Fail(IEnumerable<Error> errors)
            => new(errors?.ToImmutableArray() ?? ImmutableArray<Error>.Empty);

        /// <summary>Combine (generic): يجمع قيم النجاح إلى قائمة، أو يرجع الفشل الأول مع أخطائه.</summary>
        public static Result<IReadOnlyList<T>> Combine(params Result<T>[] results)
        {
            var list = new List<T>(results.Length);
            foreach (var r in results)
            {
                if (r.IsFailure) return Result<IReadOnlyList<T>>.Fail(r.Errors);
                list.Add(r.Value);
            }
            return Result<IReadOnlyList<T>>.Ok(list);
        }

        public void Deconstruct(out bool isSuccess, out T value, out ImmutableArray<Error> errors)
        {
            isSuccess = IsSuccess;
            value = _value;
            errors = Errors;
        }

        public override string ToString() => DebuggerDisplay;

        private string DebuggerDisplay => IsSuccess
            ? $"Result<{typeof(T).Name}>: {(_value is null ? "null" : _value)}"
            : $"Result<{typeof(T).Name}>: Failure ({Errors.Length} errors)";
    }
}