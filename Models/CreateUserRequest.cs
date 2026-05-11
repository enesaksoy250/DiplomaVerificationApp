namespace DiplomaVerificationApp.Models;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string Role,
    string? UniversityId,
    string? StudentIdentifier);
