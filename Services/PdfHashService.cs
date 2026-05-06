using System.Security.Cryptography;

namespace DiplomaVerificationApp.Services;

public sealed class PdfHashService : IPdfHashService
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const long MaxMultipartBodySizeBytes = MaxFileSizeBytes + 1024 * 1024;

    public async Task<PdfHashResult> CreateHashAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new PdfValidationException("PDF dosyasi zorunludur.");
        }

        if (file.Length == 0)
        {
            throw new PdfValidationException("Bos PDF dosyasi kabul edilmez.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new PdfValidationException("PDF dosyasi en fazla 10 MB olabilir.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new PdfValidationException("Yalnizca PDF dosyalari kabul edilir.");
        }

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream((int)file.Length);
        await stream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        if (!HasPdfSignature(bytes))
        {
            throw new PdfValidationException("Dosya icerigi gecerli bir PDF olarak algilanmadi.");
        }

        var hashBytes = SHA256.HashData(bytes);
        var hexHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new PdfHashResult($"0x{hexHash}", hashBytes);
    }

    private static bool HasPdfSignature(byte[] bytes)
    {
        if (bytes.Length < 5)
        {
            return false;
        }

        return bytes[0] == '%'
            && bytes[1] == 'P'
            && bytes[2] == 'D'
            && bytes[3] == 'F'
            && bytes[4] == '-';
    }
}
