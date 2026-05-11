namespace DiplomaVerificationApp.Models;

public sealed record BlockchainVerificationResult(
    bool Exists,
    long Timestamp,
    string? RegisteredAtUtc,
    string? TransactionHash,
    string Network,
    string? IssuerId,
    string? SignatureHash,
    string? RegisteredBy);
