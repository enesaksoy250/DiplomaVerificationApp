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
                CreatedAtUtc,
                UniversityId,
                UniversityName,
                StudentIdentifier,
                StoredFilePath,
                Signature,
                SignatureHash,
                IssuerId,
                RegisteredByUserId,
                IssuedAtUtc
            )
            VALUES (
                $pdfHash,
                $transactionHash,
                $network,
                $contractAddress,
                $blockchainTimestamp,
                $registeredAtUtc,
                $createdAtUtc,
                $universityId,
                $universityName,
                $studentIdentifier,
                $storedFilePath,
                $signature,
                $signatureHash,
                $issuerId,
                $registeredByUserId,
                $issuedAtUtc
            )
            ON CONFLICT(PdfHash) DO UPDATE SET
                TransactionHash = excluded.TransactionHash,
                Network = excluded.Network,
                ContractAddress = excluded.ContractAddress,
                BlockchainTimestamp = excluded.BlockchainTimestamp,
                RegisteredAtUtc = excluded.RegisteredAtUtc,
                UniversityId = excluded.UniversityId,
                UniversityName = excluded.UniversityName,
                StudentIdentifier = excluded.StudentIdentifier,
                StoredFilePath = excluded.StoredFilePath,
                Signature = excluded.Signature,
                SignatureHash = excluded.SignatureHash,
                IssuerId = excluded.IssuerId,
                RegisteredByUserId = excluded.RegisteredByUserId,
                IssuedAtUtc = excluded.IssuedAtUtc;
            """;
        command.Parameters.AddWithValue("$pdfHash", record.PdfHash);
        command.Parameters.AddWithValue("$transactionHash", record.TransactionHash);
        command.Parameters.AddWithValue("$network", record.Network);
        command.Parameters.AddWithValue("$contractAddress", record.ContractAddress);
        command.Parameters.AddWithValue("$blockchainTimestamp", record.BlockchainTimestamp);
        command.Parameters.AddWithValue("$registeredAtUtc", record.RegisteredAtUtc);
        command.Parameters.AddWithValue("$createdAtUtc", record.CreatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$universityId", ToDbValue(record.UniversityId));
        command.Parameters.AddWithValue("$universityName", ToDbValue(record.UniversityName));
        command.Parameters.AddWithValue("$studentIdentifier", ToDbValue(record.StudentIdentifier));
        command.Parameters.AddWithValue("$storedFilePath", ToDbValue(record.StoredFilePath));
        command.Parameters.AddWithValue("$signature", ToDbValue(record.Signature));
        command.Parameters.AddWithValue("$signatureHash", ToDbValue(record.SignatureHash));
        command.Parameters.AddWithValue("$issuerId", ToDbValue(record.IssuerId));
        command.Parameters.AddWithValue("$registeredByUserId", ToDbValue(record.RegisteredByUserId));
        command.Parameters.AddWithValue("$issuedAtUtc", ToDbValue(record.IssuedAtUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)));

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
                CreatedAtUtc,
                UniversityId,
                UniversityName,
                StudentIdentifier,
                StoredFilePath,
                Signature,
                SignatureHash,
                IssuerId,
                RegisteredByUserId,
                IssuedAtUtc
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

        return ReadRecord(reader);
    }

    public async Task<IReadOnlyList<DiplomaRecord>> GetByStudentIdentifierAsync(
        string studentIdentifier,
        CancellationToken cancellationToken)
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
                CreatedAtUtc,
                UniversityId,
                UniversityName,
                StudentIdentifier,
                StoredFilePath,
                Signature,
                SignatureHash,
                IssuerId,
                RegisteredByUserId,
                IssuedAtUtc
            FROM DiplomaRecords
            WHERE StudentIdentifier = $studentIdentifier
            ORDER BY CreatedAtUtc DESC;
            """;
        command.Parameters.AddWithValue("$studentIdentifier", studentIdentifier);

        var records = new List<DiplomaRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadRecord(reader));
        }

        return records;
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
                    CreatedAtUtc TEXT NOT NULL,
                    UniversityId TEXT NULL,
                    UniversityName TEXT NULL,
                    StudentIdentifier TEXT NULL,
                    StoredFilePath TEXT NULL,
                    Signature TEXT NULL,
                    SignatureHash TEXT NULL,
                    IssuerId TEXT NULL,
                    RegisteredByUserId TEXT NULL,
                    IssuedAtUtc TEXT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_DiplomaRecords_TransactionHash
                    ON DiplomaRecords(TransactionHash);
                """;

            await command.ExecuteNonQueryAsync(cancellationToken);
            await AddMissingColumnAsync(connection, "UniversityId TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "UniversityName TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "StudentIdentifier TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "StoredFilePath TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "Signature TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "SignatureHash TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "IssuerId TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "RegisteredByUserId TEXT NULL", cancellationToken);
            await AddMissingColumnAsync(connection, "IssuedAtUtc TEXT NULL", cancellationToken);

            await using var indexCommand = connection.CreateCommand();
            indexCommand.CommandText = """
                CREATE INDEX IF NOT EXISTS IX_DiplomaRecords_StudentIdentifier
                    ON DiplomaRecords(StudentIdentifier);
                """;
            await indexCommand.ExecuteNonQueryAsync(cancellationToken);
            _databaseInitialized = true;
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    private static DiplomaRecord ReadRecord(SqliteDataReader reader)
    {
        var createdAtUtc = DateTimeOffset.Parse(
            reader.GetString(6),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        DateTimeOffset? issuedAtUtc = null;
        var issuedAtRaw = GetNullableString(reader, 15);
        if (!string.IsNullOrWhiteSpace(issuedAtRaw))
        {
            issuedAtUtc = DateTimeOffset.Parse(
                issuedAtRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        return new DiplomaRecord(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt64(4),
            reader.GetString(5),
            createdAtUtc,
            GetNullableString(reader, 7),
            GetNullableString(reader, 8),
            GetNullableString(reader, 9),
            GetNullableString(reader, 10),
            GetNullableString(reader, 11),
            GetNullableString(reader, 12),
            GetNullableString(reader, 13),
            GetNullableString(reader, 14),
            issuedAtUtc);
    }

    private static async Task AddMissingColumnAsync(
        SqliteConnection connection,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        var columnName = columnDefinition.Split(' ', 2)[0];
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "PRAGMA table_info(DiplomaRecords);";

        await using var reader = await checkCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE DiplomaRecords ADD COLUMN {columnDefinition};";
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? GetNullableString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }
}
