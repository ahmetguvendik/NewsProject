namespace Shared.Messaging;

public static class Topics
{
    public static class User
    {
        public const string Registered = "user.registered";
        public const string Updated = "user.updated";
        public const string Deleted = "user.deleted";
    }
}
