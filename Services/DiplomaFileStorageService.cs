using DiplomaVerificationApp.Options;
using Microsoft.Extensions.Options;

namespace DiplomaVerificationApp.Services;

public sealed class DiplomaFileStorageService(IOptions<DiplomaFileStorageOptions> options) : IDiplomaFileStorageService
{
    private readonly string _rootPath = string.IsNullOrWhiteSpace(options.Value.RootPath)
        ? "App_Data/diplomas"
        : options.Value.RootPath;

    public async Task<string> SaveAsync(IFormFile file, string pdfHash, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);

        var safeHash = pdfHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? pdfHash[2..]
            : pdfHash;
        var path = Path.Combine(_rootPath, $"{safeHash}.pdf");

        await using var input = file.OpenReadStream();
        await using var output = File.Create(path);
        await input.CopyToAsync(output, cancellationToken);

        return path;
    }
}
