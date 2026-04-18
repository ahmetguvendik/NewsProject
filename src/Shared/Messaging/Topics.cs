namespace Shared.Messaging;

public static class Topics
{
    public static class User
    {
        public const string Registered = "user.registered";
        public const string Updated = "user.updated";
        public const string Deleted = "user.deleted";
    }

    public static class Article
    {
        public const string Created = "article.created";
        public const string Updated = "article.updated";
        public const string Deleted = "article.deleted";
        public const string Published = "article.published";
    }
}
