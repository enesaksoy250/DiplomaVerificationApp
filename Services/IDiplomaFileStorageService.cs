namespace DiplomaVerificationApp.Services;

public interface IDiplomaFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string pdfHash, CancellationToken cancellationToken);
}
