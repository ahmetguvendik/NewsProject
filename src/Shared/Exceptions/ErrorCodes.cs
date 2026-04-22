namespace Shared.Exceptions;

public static class ErrorCodes
{
    // ─── User ────────────────────────────────────────────────────
    public static class User
    {
        public const string NotFound           = "USER_NOT_FOUND";
        public const string AlreadyExists      = "USER_ALREADY_EXISTS";
        public const string InvalidCredentials = "USER_INVALID_CREDENTIALS";
        public const string NotActive          = "USER_NOT_ACTIVE";
        public const string RoleAlreadyAssigned = "USER_ROLE_ALREADY_ASSIGNED";
        public const string RoleNotFound       = "USER_ROLE_NOT_FOUND";
    }

    // ─── Article ─────────────────────────────────────────────────
    public static class Article
    {
        public const string NotFound         = "ARTICLE_NOT_FOUND";
        public const string AlreadyPublished = "ARTICLE_ALREADY_PUBLISHED";
        public const string NotPublished     = "ARTICLE_NOT_PUBLISHED";
        public const string TitleRequired    = "ARTICLE_TITLE_REQUIRED";
        public const string ContentRequired  = "ARTICLE_CONTENT_REQUIRED";
    }

    // ─── Category ────────────────────────────────────────────────
    public static class Category
    {
        public const string NotFound      = "CATEGORY_NOT_FOUND";
        public const string AlreadyExists = "CATEGORY_ALREADY_EXISTS";
        public const string NameRequired  = "CATEGORY_NAME_REQUIRED";
    }

    // ─── Tag ─────────────────────────────────────────────────────
    public static class Tag
    {
        public const string NotFound      = "TAG_NOT_FOUND";
        public const string AlreadyExists = "TAG_ALREADY_EXISTS";
        public const string NameRequired  = "TAG_NAME_REQUIRED";
    }

    // ─── Auth ────────────────────────────────────────────────────
    public static class Auth
    {
        public const string Unauthorized     = "AUTH_UNAUTHORIZED";
        public const string Forbidden        = "AUTH_FORBIDDEN";
        public const string TokenExpired     = "AUTH_TOKEN_EXPIRED";
        public const string TokenInvalid     = "AUTH_TOKEN_INVALID";
    }

    // ─── Keycloak ─────────────────────────────────────────────────
    public static class Keycloak
    {
        public const string UserCreationFailed  = "KEYCLOAK_USER_CREATION_FAILED";
        public const string UserDeletionFailed  = "KEYCLOAK_USER_DELETION_FAILED";
        public const string RoleAssignFailed    = "KEYCLOAK_ROLE_ASSIGN_FAILED";
        public const string RoleNotFound        = "KEYCLOAK_ROLE_NOT_FOUND";
    }

    // ─── General ─────────────────────────────────────────────────
    public static class General
    {
        public const string ValidationFailed  = "VALIDATION_FAILED";
        public const string Conflict          = "CONFLICT";
        public const string InternalError     = "INTERNAL_SERVER_ERROR";
        public const string NotFound          = "NOT_FOUND";
    }
}
