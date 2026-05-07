using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Options;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace DiplomaVerificationApp.Services;

public sealed class DiplomaBlockchainService(
    IOptions<BlockchainOptions> options,
    IDiplomaRecordRepository diplomaRecordRepository,
    ILogger<DiplomaBlockchainService> logger) : IDiplomaBlockchainService
{
    private const int VerificationRetryCount = 5;
    private const ulong EventLogBlockRange = 2_000;
    private static readonly TimeSpan VerificationRetryDelay = TimeSpan.FromSeconds(2);

    private readonly BlockchainOptions _options = options.Value;

    public async Task<BlockchainRegistrationResult> RegisterAsync(byte[] pdfHashBytes32, CancellationToken cancellationToken)
    {
        ValidateBytes32(pdfHashBytes32);

        var queryWeb3 = CreateQueryWeb3();
        var existing = await QueryBlockchainVerificationAsync(queryWeb3, pdfHashBytes32);
        if (existing.Exists)
        {
            throw new DuplicateDiplomaException("Bu PDF hash değeri blockchain üzerinde zaten kayıtlı.");
        }

        var web3 = CreateTransactionWeb3();
        var handler = web3.Eth.GetContractTransactionHandler<RegisterDiplomaFunction>();
        var receipt = await handler.SendRequestAndWaitForReceiptAsync(
            _options.ContractAddress,
            new RegisterDiplomaFunction { PdfHash = pdfHashBytes32 },
            cancellationToken);

        if (receipt.Status is null || receipt.Status.Value == 0)
        {
            throw new BlockchainStateVerificationException("Blockchain transaction başarısız oldu.");
        }

        var verification = await VerifyRegisteredDiplomaWithRetryAsync(pdfHashBytes32, cancellationToken);
        if (!verification.Exists)
        {
            throw new BlockchainStateVerificationException("Blockchain kaydı transaction sonrasında doğrulanamadı.");
        }

        var registeredAtUtc = verification.RegisteredAtUtc ?? DateTimeDisplayFormatter.FromUnixTimestamp(verification.Timestamp);
        await diplomaRecordRepository.SaveAsync(
            new DiplomaRecord(
                ToDisplayHash(pdfHashBytes32),
                receipt.TransactionHash,
                _options.NetworkName,
                _options.ContractAddress,
                verification.Timestamp,
                registeredAtUtc,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return new BlockchainRegistrationResult(
            receipt.TransactionHash,
            verification.Timestamp,
            registeredAtUtc,
            _options.NetworkName);
    }

    public async Task<BlockchainVerificationResult> VerifyAsync(byte[] pdfHashBytes32, CancellationToken cancellationToken)
    {
        ValidateBytes32(pdfHashBytes32);

        var web3 = CreateQueryWeb3();
        var verification = await QueryBlockchainVerificationAsync(web3, pdfHashBytes32);
        var transactionHash = verification.Exists
            ? await GetRegistrationTransactionHashAsync(web3, pdfHashBytes32, verification.Timestamp, cancellationToken)
            : null;

        return new BlockchainVerificationResult(
            verification.Exists,
            verification.Timestamp,
            verification.RegisteredAtUtc,
            transactionHash,
            _options.NetworkName);
    }

    private async Task<BlockchainVerificationResult> QueryBlockchainVerificationAsync(Web3 web3, byte[] pdfHashBytes32)
    {
        var handler = web3.Eth.GetContractQueryHandler<VerifyDiplomaFunction>();
        var output = await handler.QueryDeserializingToObjectAsync<VerifyDiplomaOutput>(
            new VerifyDiplomaFunction { PdfHash = pdfHashBytes32 },
            _options.ContractAddress);

        var timestamp = (long)output.Timestamp;
        var registeredAtUtc = output.Exists
            ? DateTimeDisplayFormatter.FromUnixTimestamp(timestamp)
            : null;

        return new BlockchainVerificationResult(
            output.Exists,
            timestamp,
            registeredAtUtc,
            null,
            _options.NetworkName);
    }

    private async Task<string?> GetRegistrationTransactionHashAsync(
        Web3 web3,
        byte[] pdfHashBytes32,
        long timestamp,
        CancellationToken cancellationToken)
    {
        var pdfHash = ToDisplayHash(pdfHashBytes32);
        var localRecord = await diplomaRecordRepository.GetByPdfHashAsync(pdfHash, cancellationToken);
        if (!string.IsNullOrWhiteSpace(localRecord?.TransactionHash))
        {
            return localRecord.TransactionHash;
        }

        return await TryFindRegistrationTransactionHashAsync(web3, pdfHashBytes32, timestamp);
    }

    private Web3 CreateQueryWeb3()
    {
        if (string.IsNullOrWhiteSpace(_options.RpcUrl))
        {
            throw new BlockchainConfigurationException("Blockchain:RpcUrl ayarı eksik.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContractAddress))
        {
            throw new BlockchainConfigurationException("Blockchain:ContractAddress ayarı eksik.");
        }

        return new Web3(_options.RpcUrl);
    }

    private Web3 CreateTransactionWeb3()
    {
        if (string.IsNullOrWhiteSpace(_options.RpcUrl))
        {
            throw new BlockchainConfigurationException("Blockchain:RpcUrl ayarı eksik.");
        }

        if (string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            throw new BlockchainConfigurationException("Blockchain:PrivateKey ayarı eksik.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContractAddress))
        {
            throw new BlockchainConfigurationException("Blockchain:ContractAddress ayarı eksik.");
        }

        var account = new Account(_options.PrivateKey, _options.ChainId);
        return new Web3(account, _options.RpcUrl);
    }

    private async Task<string?> TryFindRegistrationTransactionHashAsync(Web3 web3, byte[] pdfHashBytes32, long timestamp)
    {
        try
        {
            var eventHandler = web3.Eth.GetEvent<DiplomaRegisteredEventDto>(_options.ContractAddress);
            var latestBlock = (ulong)(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
            var fromBlock = GetContractStartBlock();
            string? latestMatchingTransactionHash = null;

            while (fromBlock <= latestBlock)
            {
                var toBlock = Math.Min(fromBlock + EventLogBlockRange - 1, latestBlock);
                var filterInput = eventHandler.CreateFilterInput(
                    pdfHashBytes32,
                    fromBlock: new BlockParameter(new HexBigInteger(fromBlock)),
                    toBlock: new BlockParameter(new HexBigInteger(toBlock)));
                var logs = await eventHandler.GetAllChangesAsync(filterInput);

                var orderedLogs = logs
                    .OrderBy(log => log.Log.BlockNumber.Value)
                    .ThenBy(log => log.Log.TransactionIndex.Value)
                    .ToArray();

                var timestampMatch = orderedLogs
                    .Where(log => (long)log.Event.Timestamp == timestamp)
                    .LastOrDefault()
                    ?.Log.TransactionHash;

                if (!string.IsNullOrWhiteSpace(timestampMatch))
                {
                    return timestampMatch;
                }

                latestMatchingTransactionHash = orderedLogs.LastOrDefault()?.Log.TransactionHash
                    ?? latestMatchingTransactionHash;

                if (toBlock == latestBlock)
                {
                    break;
                }

                fromBlock = toBlock + 1;
            }

            return latestMatchingTransactionHash;
        }
        catch (Exception ex)
        {
            logger.LogDebug(
                ex,
                "DiplomaRegistered event log sorgusu başarısız oldu. ContractAddress: {ContractAddress}, ContractStartBlock: {ContractStartBlock}",
                _options.ContractAddress,
                _options.ContractStartBlock);

            return null;
        }
    }

    private ulong GetContractStartBlock()
    {
        return _options.ContractStartBlock > 0 ? _options.ContractStartBlock : 0;
    }

    private async Task<BlockchainVerificationResult> VerifyRegisteredDiplomaWithRetryAsync(
        byte[] pdfHashBytes32,
        CancellationToken cancellationToken)
    {
        BlockchainVerificationResult? latestResult = null;
        var web3 = CreateQueryWeb3();

        for (var attempt = 1; attempt <= VerificationRetryCount; attempt++)
        {
            latestResult = await QueryBlockchainVerificationAsync(web3, pdfHashBytes32);
            if (latestResult.Exists)
            {
                return latestResult;
            }

            if (attempt < VerificationRetryCount)
            {
                await Task.Delay(VerificationRetryDelay, cancellationToken);
            }
        }

        return latestResult ?? new BlockchainVerificationResult(false, 0, null, null, _options.NetworkName);
    }

    private static void ValidateBytes32(byte[] pdfHashBytes32)
    {
        if (pdfHashBytes32.Length != 32)
        {
            throw new ArgumentException("Hash değeri bytes32 için tam 32 byte olmalıdır.", nameof(pdfHashBytes32));
        }
    }

    private static string ToDisplayHash(byte[] pdfHashBytes32)
    {
        return $"0x{Convert.ToHexString(pdfHashBytes32).ToLowerInvariant()}";
    }
}
