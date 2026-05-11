namespace DiplomaVerificationApp.Models;

public sealed record UniversityResponse(
    string Id,
    string Name,
    bool HasKeyPair,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? KeyCreatedAtUtc);
