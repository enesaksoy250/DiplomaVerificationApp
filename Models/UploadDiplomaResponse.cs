namespace DiplomaVerificationApp.Models;

public sealed record UploadDiplomaResponse(
    string Status,
    string PdfHash,
    string TransactionHash,
    long Timestamp,
    string RegisteredAtUtc,
    string Network,
    string UniversityName,
    string StudentIdentifier,
    string SignatureHash,
    bool SignatureValid,
    string VerificationUrl,
    string QrCodeDataUrl);
