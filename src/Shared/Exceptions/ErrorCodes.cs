namespace Shared.Exceptions;

public static class ErrorCodes
{
    public static class User
    {
        public const string NotFound                     = "USER_NOT_FOUND";
        public const string AlreadyExists                = "USER_ALREADY_EXISTS";
        public const string RoleAlreadyAssigned          = "USER_ROLE_ALREADY_ASSIGNED";
        public const string RoleNotFound                 = "USER_ROLE_NOT_FOUND";
        public const string LastAdminCannotBeRemoved     = "USER_LAST_ADMIN_CANNOT_BE_REMOVED";
        public const string LastAdminCannotBeDeactivated = "USER_LAST_ADMIN_CANNOT_BE_DEACTIVATED";
        public const string CannotDeactivateSelf         = "USER_CANNOT_DEACTIVATE_SELF";
    }

    public static class Article
    {
        public const string NotFound         = "ARTICLE_NOT_FOUND";
        public const string AlreadyPublished = "ARTICLE_ALREADY_PUBLISHED";
    }

    public static class Media
    {
        public const string TypeNotAllowed = "MEDIA_TYPE_NOT_ALLOWED";
        public const string TooLarge       = "MEDIA_TOO_LARGE";
        public const string NotFound       = "MEDIA_NOT_FOUND";
        public const string CorruptContent = "MEDIA_CORRUPT_CONTENT";
        public const string InvalidKey     = "MEDIA_INVALID_KEY";
    }

    public static class Category
    {
        public const string NotFound      = "CATEGORY_NOT_FOUND";
        public const string AlreadyExists = "CATEGORY_ALREADY_EXISTS";
        public const string InUse         = "CATEGORY_IN_USE";
    }

    public static class Tag
    {
        public const string NotFound      = "TAG_NOT_FOUND";
        public const string AlreadyExists = "TAG_ALREADY_EXISTS";
    }

    public static class Auth
    {
        public const string Unauthorized          = "AUTH_UNAUTHORIZED";
        public const string Forbidden             = "AUTH_FORBIDDEN";
        public const string AccountDisabled       = "AUTH_ACCOUNT_DISABLED";
        public const string TokenExpired          = "AUTH_TOKEN_EXPIRED";
        public const string TokenInvalid          = "AUTH_TOKEN_INVALID";
        public const string TokenStale            = "AUTH_TOKEN_STALE";
    }

    public static class Keycloak
    {
        public const string UserCreationFailed = "KEYCLOAK_USER_CREATION_FAILED";
        public const string UserDeletionFailed = "KEYCLOAK_USER_DELETION_FAILED";
        public const string UserDisableFailed  = "KEYCLOAK_USER_DISABLE_FAILED";
        public const string UserEnableFailed   = "KEYCLOAK_USER_ENABLE_FAILED";
        public const string RoleAssignFailed   = "KEYCLOAK_ROLE_ASSIGN_FAILED";
        public const string RoleNotFound       = "KEYCLOAK_ROLE_NOT_FOUND";
        public const string AdminAuthFailed    = "KEYCLOAK_ADMIN_AUTH_FAILED";
        public const string EventsReadFailed   = "KEYCLOAK_EVENTS_READ_FAILED";
        public const string VerifyMailFailed   = "KEYCLOAK_VERIFY_MAIL_FAILED";
    }

    public static class General
    {
        public const string ValidationFailed    = "VALIDATION_FAILED";
        public const string InternalError       = "INTERNAL_SERVER_ERROR";
        public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    }
}
