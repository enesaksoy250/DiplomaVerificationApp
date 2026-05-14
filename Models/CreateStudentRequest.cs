namespace DiplomaVerificationApp.Models;

public sealed record CreateStudentRequest(
    string Email,
    string Password,
    string StudentIdentifier);
