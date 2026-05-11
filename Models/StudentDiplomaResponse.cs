namespace DiplomaVerificationApp.Models;

public sealed record StudentDiplomaResponse(
    string PdfHash,
    string TransactionHash,
    string? UniversityName,
    string? SignatureHash,
    string RegisteredAtUtc);
