namespace DiplomaVerificationApp.Services;

public interface IPdfHashService
{
    Task<PdfHashResult> CreateHashAsync(IFormFile file, CancellationToken cancellationToken);
}

public sealed record PdfHashResult(string HexHash, byte[] Bytes32);
