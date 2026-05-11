namespace DiplomaVerificationApp.Data;

public sealed class University
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string? PublicKeyPem { get; set; }

    public string? ProtectedPrivateKeyPem { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? KeyCreatedAtUtc { get; set; }
}
