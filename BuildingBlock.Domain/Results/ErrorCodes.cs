namespace BuildingBlock.Domain.Results
{
    public static class ErrorCodes
    {
        public static class User
        {
            public const string NotFound = "User.NotFound";
            public const string Inactive = "User.Inactive";
            public const string InvalidName = "User.InvalidName";
        }

        public static class Common
        {
            public const string BadRequest = "Common.BadRequest";
            public const string RateLimited = "Common.RateLimited";
            public const string InfraTimeout = "Common.Infra.Timeout";
            public const string Conflict = "Common.Conflict";
            public const string Unauthorized = "Common.Unauthorized";
            public const string Forbidden = "Common.Forbidden";
        }
    }
}