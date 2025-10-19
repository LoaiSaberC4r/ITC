using BuildingBlock.Domain.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlock.Domain.Exceptions
{
    public static class ExceptionToErrorMapper
    {
        public static Error ToError(Exception ex) => ex switch
        {
            ArgumentException aex
                => Error.Domain(ErrorCodes.Common.BadRequest, aex.Message, source: "Exception"),
            KeyNotFoundException knf
                => Error.NotFound(ErrorCodes.User.NotFound, knf.Message, source: "Exception"),
            UnauthorizedAccessException
                => Error.Security(ErrorCodes.Common.Unauthorized, "Unauthorized.", source: "Exception"),
            OperationCanceledException
                => Error.Infra(ErrorCodes.Common.InfraTimeout, "Request was canceled.", retryAfter: TimeSpan.FromSeconds(1)),
#if NET8_0_OR_GREATER
            DbUpdateConcurrencyException
                => Error.Conflict(ErrorCodes.Common.Conflict, "Concurrency conflict.", source: "EFCore"),
            DbUpdateException dbex when IsUniqueViolation(dbex)
                => Error.Conflict(ErrorCodes.Common.Conflict, "Unique constraint violated.", source: "EFCore"),
#endif
            HttpRequestException hex
                => Error.Infra(ErrorCodes.Common.InfraTimeout, hex.Message, source: "HTTP"),
            _ => Error.Unknown("Unknown.Exception", "Unhandled server error.", source: ex.GetType().FullName)
        };

#if NET8_0_OR_GREATER

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException sql && sql.Number is 2627 or 2601;

#endif
    }
}