using System.Security.Cryptography;
using System.Text;
using DiplomaVerificationApp.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace DiplomaVerificationApp.Services;

public sealed class UniversityKeyService(
    ApplicationDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider) : IUniversityKeyService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("UniversityPrivateKeys.v1");

    public async Task GenerateKeyPairAsync(string universityId, CancellationToken cancellationToken)
    {
        var university = await dbContext.Universities.SingleOrDefaultAsync(item => item.Id == universityId, cancellationToken)
            ?? throw new InvalidOperationException("Üniversite bulunamadı.");

        using var rsa = RSA.Create(3072);
        university.PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        university.ProtectedPrivateKeyPem = _protector.Protect(rsa.ExportPkcs8PrivateKeyPem());
        university.KeyCreatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<DiplomaSignatureResult> SignDiplomaAsync(
        University university,
        string pdfHash,
        string studentIdentifier,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(university.ProtectedPrivateKeyPem))
        {
            throw new InvalidOperationException("Üniversite için private key üretilmemiş.");
        }

        var issuedAtUtc = DateTimeOffset.UtcNow;
        var payload = CreatePayload(pdfHash, university.Id, studentIdentifier, issuedAtUtc);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_protector.Unprotect(university.ProtectedPrivateKeyPem));
        var signatureBytes = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureHashBytes = SHA256.HashData(signatureBytes);
        var issuerIdBytes = SHA256.HashData(Encoding.UTF8.GetBytes(university.Id));

        return Task.FromResult(new DiplomaSignatureResult(
            $"0x{Convert.ToHexString(issuerIdBytes).ToLowerInvariant()}",
            issuerIdBytes,
            Convert.ToBase64String(signatureBytes),
            $"0x{Convert.ToHexString(signatureHashBytes).ToLowerInvariant()}",
            signatureHashBytes,
            issuedAtUtc));
    }

    public bool VerifyDiplomaSignature(
        University university,
        string pdfHash,
        string studentIdentifier,
        DateTimeOffset issuedAtUtc,
        string signature)
    {
        if (string.IsNullOrWhiteSpace(university.PublicKeyPem) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(university.PublicKeyPem);
            return rsa.VerifyData(
                CreatePayload(pdfHash, university.Id, studentIdentifier, issuedAtUtc),
                Convert.FromBase64String(signature),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] CreatePayload(
        string pdfHash,
        string universityId,
        string studentIdentifier,
        DateTimeOffset issuedAtUtc)
    {
        return Encoding.UTF8.GetBytes($"{pdfHash}|{universityId}|{studentIdentifier}|{issuedAtUtc.UtcDateTime:O}");
    }
}
