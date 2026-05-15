using System.Security.Claims;
using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Options;
using DiplomaVerificationApp.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DiplomaVerificationApp.Services;

public sealed class DiplomaWorkflowService(
    IPdfHashService pdfHashService,
    IDiplomaBlockchainService blockchainService,
    IVerificationLinkService verificationLinkService,
    IQrCodeService qrCodeService,
    IDiplomaRecordRepository diplomaRecordRepository,
    IDiplomaFileStorageService fileStorageService,
    IUniversityKeyService universityKeyService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<BlockchainOptions> blockchainOptions) : IDiplomaWorkflowService
{
    public async Task<UploadDiplomaResponse> UploadAsync(
        IFormFile file,
        string studentIdentifier,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var currentUser = await userManager.GetUserAsync(user);
        if (currentUser?.UniversityId is null)
        {
            throw new UnauthorizedAccessException("Kullanıcı bir üniversiteye bağlı değil.");
        }

        if (string.IsNullOrWhiteSpace(studentIdentifier))
        {
            throw new InvalidOperationException("Öğrenci numarası zorunludur.");
        }

        var normalizedStudentIdentifier = studentIdentifier.Trim();
        var university = await dbContext.Universities.SingleOrDefaultAsync(
            item => item.Id == currentUser.UniversityId,
            cancellationToken);
        if (university is null)
        {
            throw new InvalidOperationException("Kullanıcı bir üniversiteye bağlı değil.");
        }

        await EnsureStudentBelongsToUniversityAsync(
            university.Id,
            normalizedStudentIdentifier,
            cancellationToken);

        var pdfHash = await pdfHashService.CreateHashAsync(file, cancellationToken);
        var signature = await universityKeyService.SignDiplomaAsync(
            university,
            pdfHash.HexHash,
            normalizedStudentIdentifier,
            cancellationToken);
        var blockchainResult = await blockchainService.RegisterAsync(
            pdfHash.Bytes32,
            signature.IssuerIdBytes32,
            signature.SignatureHashBytes32,
            cancellationToken);
        var storedFilePath = await fileStorageService.SaveAsync(file, pdfHash.HexHash, cancellationToken);
        var verificationUrl = verificationLinkService.Build(httpContext, pdfHash.HexHash);
        var qrCodeDataUrl = qrCodeService.CreateDataUrl(verificationUrl);

        await diplomaRecordRepository.SaveAsync(
            new DiplomaRecord(
                pdfHash.HexHash,
                blockchainResult.TransactionHash,
                blockchainResult.Network,
                blockchainOptions.Value.ContractAddress,
                blockchainResult.Timestamp,
                blockchainResult.RegisteredAtUtc,
                DateTimeOffset.UtcNow,
                university.Id,
                university.Name,
                normalizedStudentIdentifier,
                storedFilePath,
                signature.Signature,
                signature.SignatureHash,
                signature.IssuerId,
                currentUser.Id,
                signature.IssuedAtUtc),
            cancellationToken);

        return new UploadDiplomaResponse(
            "Geçerli Diploma",
            pdfHash.HexHash,
            blockchainResult.TransactionHash,
            blockchainResult.Timestamp,
            blockchainResult.RegisteredAtUtc,
            blockchainResult.Network,
            university.Name,
            normalizedStudentIdentifier,
            signature.SignatureHash,
            true,
            verificationUrl,
            qrCodeDataUrl);
    }

    public async Task<VerifyDiplomaResponse> VerifyFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var pdfHash = await pdfHashService.CreateHashAsync(file, cancellationToken);
        var verification = await blockchainService.VerifyAsync(pdfHash.Bytes32, cancellationToken);

        return await ToVerifyResponseAsync(pdfHash.HexHash, verification, "Geçersiz Diploma", cancellationToken);
    }

    public async Task<VerifyDiplomaResponse> VerifyHashAsync(string hash, CancellationToken cancellationToken)
    {
        var pdfHash = HexHashConverter.NormalizeDisplayHash(hash);
        var hashBytes = HexHashConverter.ToBytes32(pdfHash);
        var verification = await blockchainService.VerifyAsync(hashBytes, cancellationToken);

        return await ToVerifyResponseAsync(pdfHash, verification, "Blockchain Kaydı Bulunamadı", cancellationToken);
    }

    private async Task EnsureStudentBelongsToUniversityAsync(
        string universityId,
        string studentIdentifier,
        CancellationToken cancellationToken)
    {
        var universityStudents = await userManager.GetUsersInRoleAsync(AppRoles.Student);
        var studentExists = universityStudents.Any(student =>
            student.UniversityId == universityId &&
            string.Equals(student.StudentIdentifier, studentIdentifier, StringComparison.OrdinalIgnoreCase));
        if (!studentExists)
        {
            throw new InvalidOperationException("Seçilen öğrenci bu üniversiteye kayıtlı değil.");
        }
    }

    private async Task<VerifyDiplomaResponse> ToVerifyResponseAsync(
        string pdfHash,
        BlockchainVerificationResult verification,
        string missingStatus,
        CancellationToken cancellationToken)
    {
        if (!verification.Exists)
        {
            return new VerifyDiplomaResponse(
                missingStatus,
                pdfHash,
                false,
                null,
                null,
                verification.TransactionHash,
                verification.Network,
                null,
                null,
                null,
                false);
        }

        var record = await diplomaRecordRepository.GetByPdfHashAsync(pdfHash, cancellationToken);
        var university = record?.UniversityId is null
            ? null
            : await dbContext.Universities.SingleOrDefaultAsync(item => item.Id == record.UniversityId, cancellationToken);
        var signatureValid = record is not null
            && university is not null
            && record.IssuedAtUtc is not null
            && string.Equals(record.IssuerId, verification.IssuerId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(record.SignatureHash, verification.SignatureHash, StringComparison.OrdinalIgnoreCase)
            && universityKeyService.VerifyDiplomaSignature(
                university,
                pdfHash,
                record.StudentIdentifier ?? string.Empty,
                record.IssuedAtUtc.Value,
                record.Signature ?? string.Empty);

        return new VerifyDiplomaResponse(
            signatureValid ? "Geçerli Diploma" : "Geçersiz Diploma",
            pdfHash,
            signatureValid,
            verification.Timestamp,
            verification.RegisteredAtUtc,
            verification.TransactionHash ?? record?.TransactionHash,
            verification.Network,
            record?.UniversityName,
            record?.StudentIdentifier,
            verification.SignatureHash ?? record?.SignatureHash,
            signatureValid);
    }
}
