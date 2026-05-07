namespace DiplomaVerificationApp.Models;

public sealed record DiplomaRecord(
    string PdfHash,
    string TransactionHash,
    string Network,
    string ContractAddress,
    long BlockchainTimestamp,
    string RegisteredAtUtc,
    DateTimeOffset CreatedAtUtc);
