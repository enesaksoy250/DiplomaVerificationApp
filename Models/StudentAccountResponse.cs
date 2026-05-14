namespace DiplomaVerificationApp.Models;

public sealed record StudentAccountResponse(
    string Id,
    string? Email,
    string StudentIdentifier,
    int DiplomaCount,
    bool HasDiploma,
    string? LastRegisteredAtUtc);
