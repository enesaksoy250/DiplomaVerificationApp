namespace DiplomaVerificationApp.Models;

public sealed record DiplomaRecord(
    string PdfHash,
    string TransactionHash,
    string Network,
    string ContractAddress,
    long BlockchainTimestamp,
    string RegisteredAtUtc,
    DateTimeOffset CreatedAtUtc,
    string? UniversityId,
    string? UniversityName,
    string? StudentIdentifier,
    string? StoredFilePath,
    string? Signature,
    string? SignatureHash,
    string? IssuerId,
    string? RegisteredByUserId,
    DateTimeOffset? IssuedAtUtc);
