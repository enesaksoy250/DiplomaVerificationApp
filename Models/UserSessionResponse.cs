namespace DiplomaVerificationApp.Models;

public sealed record UserSessionResponse(
    bool Authenticated,
    string? Email,
    IReadOnlyList<string> Roles,
    string? UniversityId,
    string? StudentIdentifier);
