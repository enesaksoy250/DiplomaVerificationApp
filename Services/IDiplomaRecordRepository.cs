using DiplomaVerificationApp.Models;

namespace DiplomaVerificationApp.Services;

public interface IDiplomaRecordRepository
{
    Task SaveAsync(DiplomaRecord record, CancellationToken cancellationToken);

    Task<DiplomaRecord?> GetByPdfHashAsync(string pdfHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiplomaRecord>> GetByStudentIdentifierAsync(string studentIdentifier, CancellationToken cancellationToken);
}
