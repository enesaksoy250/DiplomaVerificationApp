namespace DiplomaVerificationApp.Services;

public static class HexHashConverter
{
    public static byte[] ToBytes32(string hexHash)
    {
        var normalized = NormalizeHexHash(hexHash);
        return Convert.FromHexString(normalized);
    }

    public static string NormalizeDisplayHash(string hexHash)
    {
        return $"0x{NormalizeHexHash(hexHash).ToLowerInvariant()}";
    }

    private static string NormalizeHexHash(string hexHash)
    {
        if (string.IsNullOrWhiteSpace(hexHash))
        {
            throw new PdfValidationException("Hash değeri zorunludur.");
        }

        var normalized = hexHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? hexHash[2..]
            : hexHash;

        if (normalized.Length != 64)
        {
            throw new PdfValidationException("SHA256 hash değeri 32 byte uzunluğunda olmalıdır.");
        }

        if (!normalized.All(Uri.IsHexDigit))
        {
            throw new PdfValidationException("Hash değeri hexadecimal formatta olmalıdır.");
        }

        return normalized;
    }
}
