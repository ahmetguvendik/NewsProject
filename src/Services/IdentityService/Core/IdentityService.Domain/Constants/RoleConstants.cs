namespace IdentityService.Domain.Constants;

public static class RoleConstants
{
    public const string Admin = "admin";
    public const string Editor = "editor";
    public const string User = "user";

    // DB'deki seed ID'leriyle birebir eşleşiyor
    public static readonly Guid AdminId  = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid EditorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid UserId   = Guid.Parse("00000000-0000-0000-0000-000000000003");

    /// <summary>
    /// Rol adına karşılık gelen Guid'i döner.
    /// Bilinmeyen rol adı gelirse null döner; exception fırlatmak çağırana bırakılmıştır.
    /// </summary>
    public static Guid? TryGetId(string roleName) => roleName.ToLower() switch
    {
        Admin  => AdminId,
        Editor => EditorId,
        User   => UserId,
        _      => null
    };

    /// <summary>
    /// Geriye dönük uyumluluk için korunuyor — yeni kod TryGetId kullanmalı.
    /// Bilinmeyen rol → InvalidOperationException (Application katmanında yakalanır).
    /// </summary>
    public static Guid GetId(string roleName) =>
        TryGetId(roleName) ?? throw new InvalidOperationException($"Unknown role: '{roleName}'");
}
