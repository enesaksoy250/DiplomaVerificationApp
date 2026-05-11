using DiplomaVerificationApp.Models;

namespace DiplomaVerificationApp.Services;

public interface IDiplomaBlockchainService
{
    Task<BlockchainRegistrationResult> RegisterAsync(
        byte[] pdfHashBytes32,
        byte[] issuerIdBytes32,
        byte[] signatureHashBytes32,
        CancellationToken cancellationToken);

    Task<BlockchainVerificationResult> VerifyAsync(byte[] pdfHashBytes32, CancellationToken cancellationToken);
}
