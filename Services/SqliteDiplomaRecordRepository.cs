using System.Globalization;
using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace DiplomaVerificationApp.Services;

public sealed class SqliteDiplomaRecordRepository(
    IOptions<DiplomaRecordStorageOptions> options) : IDiplomaRecordRepository
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(options.Value.ConnectionString)
        ? "Data Source=diploma-records.db"
        : options.Value.ConnectionString;
    private readonly SemaphoreSlim _databaseLock = new(1, 1);
    private bool _databaseInitialized;

    public async Task SaveAsync(DiplomaRecord record, CancellationToken cancellationToken)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DiplomaRecords (
                PdfHash,
                TransactionHash,
                Network,
                ContractAddress,
                BlockchainTimestamp,
                RegisteredAtUtc,
                CreatedAtUtc
            )
            VALUES (
                $pdfHash,
                $transactionHash,
                $network,
                $contractAddress,
                $blockchainTimestamp,
                $registeredAtUtc,
                $createdAtUtc
            )
            ON CONFLICT(PdfHash) DO UPDATE SET
                TransactionHash = excluded.TransactionHash,
                Network = excluded.Network,
                ContractAddress = excluded.ContractAddress,
                BlockchainTimestamp = excluded.BlockchainTimestamp,
                RegisteredAtUtc = excluded.RegisteredAtUtc;
            """;
        command.Parameters.AddWithValue("$pdfHash", record.PdfHash);
        command.Parameters.AddWithValue("$transactionHash", record.TransactionHash);
        command.Parameters.AddWithValue("$network", record.Network);
        command.Parameters.AddWithValue("$contractAddress", record.ContractAddress);
        command.Parameters.AddWithValue("$blockchainTimestamp", record.BlockchainTimestamp);
        command.Parameters.AddWithValue("$registeredAtUtc", record.RegisteredAtUtc);
        command.Parameters.AddWithValue("$createdAtUtc", record.CreatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DiplomaRecord?> GetByPdfHashAsync(string pdfHash, CancellationToken cancellationToken)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                PdfHash,
                TransactionHash,
                Network,
                ContractAddress,
                BlockchainTimestamp,
                RegisteredAtUtc,
                CreatedAtUtc
            FROM DiplomaRecords
            WHERE PdfHash = $pdfHash
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$pdfHash", pdfHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var createdAtUtc = DateTimeOffset.Parse(
            reader.GetString(6),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        return new DiplomaRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt64(4),
            reader.GetString(5),
            createdAtUtc);
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_databaseInitialized)
        {
            return;
        }

        await _databaseLock.WaitAsync(cancellationToken);
        try
        {
            if (_databaseInitialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS DiplomaRecords (
                    PdfHash TEXT PRIMARY KEY,
                    TransactionHash TEXT NOT NULL,
                    Network TEXT NOT NULL,
                    ContractAddress TEXT NOT NULL,
                    BlockchainTimestamp INTEGER NOT NULL,
                    RegisteredAtUtc TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_DiplomaRecords_TransactionHash
                    ON DiplomaRecords(TransactionHash);
                """;

            await command.ExecuteNonQueryAsync(cancellationToken);
            _databaseInitialized = true;
        }
        finally
        {
            _databaseLock.Release();
        }
    }
}
