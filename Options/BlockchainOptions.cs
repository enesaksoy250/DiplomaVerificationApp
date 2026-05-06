namespace DiplomaVerificationApp.Options;

public sealed class BlockchainOptions
{
    public string NetworkName { get; init; } = "Sepolia";

    public long ChainId { get; init; } = 11155111;

    public string RpcUrl { get; init; } = string.Empty;

    public string PrivateKey { get; init; } = string.Empty;

    public string ContractAddress { get; init; } = string.Empty;

    public string VerificationBaseUrl { get; init; } = string.Empty;
}
