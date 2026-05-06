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
            throw new PdfValidationException("Hash degeri zorunludur.");
        }

        var normalized = hexHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? hexHash[2..]
            : hexHash;

        if (normalized.Length != 64)
        {
            throw new PdfValidationException("SHA256 hash degeri 32 byte uzunlugunda olmalidir.");
        }

        if (!normalized.All(Uri.IsHexDigit))
        {
            throw new PdfValidationException("Hash degeri hexadecimal formatta olmalidir.");
        }

        return normalized;
    }
}
