using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Options;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace DiplomaVerificationApp.Services;

public sealed class DiplomaBlockchainService(IOptions<BlockchainOptions> options) : IDiplomaBlockchainService
{
    private readonly BlockchainOptions _options = options.Value;

    public async Task<BlockchainRegistrationResult> RegisterAsync(byte[] pdfHashBytes32, CancellationToken cancellationToken)
    {
        ValidateBytes32(pdfHashBytes32);

        var existing = await VerifyAsync(pdfHashBytes32, cancellationToken);
        if (existing.Exists)
        {
            throw new DuplicateDiplomaException("Bu PDF hash degeri blockchain uzerinde zaten kayitli.");
        }

        var web3 = CreateTransactionWeb3();
        var handler = web3.Eth.GetContractTransactionHandler<RegisterDiplomaFunction>();
        var receipt = await handler.SendRequestAndWaitForReceiptAsync(
            _options.ContractAddress,
            new RegisterDiplomaFunction { PdfHash = pdfHashBytes32 },
            cancellationToken);

        var verification = await VerifyAsync(pdfHashBytes32, cancellationToken);
        if (!verification.Exists)
        {
            throw new InvalidOperationException("Blockchain kaydi transaction sonrasinda dogrulanamadi.");
        }

        return new BlockchainRegistrationResult(
            receipt.TransactionHash,
            verification.Timestamp,
            verification.RegisteredAtUtc ?? DateTimeDisplayFormatter.FromUnixTimestamp(verification.Timestamp),
            _options.NetworkName);
    }

    public async Task<BlockchainVerificationResult> VerifyAsync(byte[] pdfHashBytes32, CancellationToken cancellationToken)
    {
        ValidateBytes32(pdfHashBytes32);

        var web3 = CreateQueryWeb3();
        var handler = web3.Eth.GetContractQueryHandler<VerifyDiplomaFunction>();
        var output = await handler.QueryDeserializingToObjectAsync<VerifyDiplomaOutput>(
            new VerifyDiplomaFunction { PdfHash = pdfHashBytes32 },
            _options.ContractAddress);

        var timestamp = (long)output.Timestamp;
        var registeredAtUtc = output.Exists
            ? DateTimeDisplayFormatter.FromUnixTimestamp(timestamp)
            : null;
        var transactionHash = output.Exists
            ? await TryFindRegistrationTransactionHashAsync(web3, pdfHashBytes32)
            : null;

        return new BlockchainVerificationResult(
            output.Exists,
            timestamp,
            registeredAtUtc,
            transactionHash,
            _options.NetworkName);
    }

    private Web3 CreateQueryWeb3()
    {
        if (string.IsNullOrWhiteSpace(_options.RpcUrl))
        {
            throw new BlockchainConfigurationException("Blockchain:RpcUrl ayari eksik.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContractAddress))
        {
            throw new BlockchainConfigurationException("Blockchain:ContractAddress ayari eksik.");
        }

        return new Web3(_options.RpcUrl);
    }

    private Web3 CreateTransactionWeb3()
    {
        if (string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            throw new BlockchainConfigurationException("Blockchain:PrivateKey ayari eksik.");
        }

        var account = new Account(_options.PrivateKey, _options.ChainId);
        return new Web3(account, _options.RpcUrl);
    }

    private async Task<string?> TryFindRegistrationTransactionHashAsync(Web3 web3, byte[] pdfHashBytes32)
    {
        try
        {
            var eventHandler = web3.Eth.GetEvent<DiplomaRegisteredEventDto>(_options.ContractAddress);
            var filterInput = eventHandler.CreateFilterInput(pdfHashBytes32);
            var logs = await eventHandler.GetAllChangesAsync(filterInput);

            return logs
                .OrderBy(log => log.Log.BlockNumber.Value)
                .LastOrDefault()
                ?.Log.TransactionHash;
        }
        catch
        {
            return null;
        }
    }

    private static void ValidateBytes32(byte[] pdfHashBytes32)
    {
        if (pdfHashBytes32.Length != 32)
        {
            throw new ArgumentException("Hash degeri bytes32 icin tam 32 byte olmalidir.", nameof(pdfHashBytes32));
        }
    }
}
