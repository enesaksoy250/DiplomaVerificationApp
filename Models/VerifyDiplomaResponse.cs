namespace DiplomaVerificationApp.Models;

public sealed record VerifyDiplomaResponse(
    string Status,
    string PdfHash,
    bool Exists,
    long? Timestamp,
    string? RegisteredAtUtc,
    string? TransactionHash,
    string Network);
