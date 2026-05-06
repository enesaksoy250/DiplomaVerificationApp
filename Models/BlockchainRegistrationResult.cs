namespace DiplomaVerificationApp.Models;

public sealed record BlockchainRegistrationResult(
    string TransactionHash,
    long Timestamp,
    string RegisteredAtUtc,
    string Network);
