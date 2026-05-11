using DiplomaVerificationApp.Data;

namespace DiplomaVerificationApp.Services;

public interface IUniversityKeyService
{
    Task GenerateKeyPairAsync(string universityId, CancellationToken cancellationToken);

    Task<DiplomaSignatureResult> SignDiplomaAsync(
        University university,
        string pdfHash,
        string studentIdentifier,
        CancellationToken cancellationToken);

    bool VerifyDiplomaSignature(
        University university,
        string pdfHash,
        string studentIdentifier,
        DateTimeOffset issuedAtUtc,
        string signature);
}
