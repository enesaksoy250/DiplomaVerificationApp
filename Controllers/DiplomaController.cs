using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Options;
using DiplomaVerificationApp.Security;
using DiplomaVerificationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Route("")]
public sealed class DiplomaController(
    IPdfHashService pdfHashService,
    IDiplomaBlockchainService blockchainService,
    IVerificationLinkService verificationLinkService,
    IQrCodeService qrCodeService,
    IDiplomaRecordRepository diplomaRecordRepository,
    IDiplomaFileStorageService fileStorageService,
    IUniversityKeyService universityKeyService,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<BlockchainOptions> blockchainOptions) : ControllerBase
{
    [Authorize(Roles = AppRoles.University)]
    [HttpPost("upload")]
    [RequestSizeLimit(PdfHashService.MaxMultipartBodySizeBytes)]
    public async Task<ActionResult<UploadDiplomaResponse>> Upload(
        IFormFile file,
        [FromForm] string studentIdentifier,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser?.UniversityId is null)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(studentIdentifier))
            {
                return BadRequest(new { status = "Geçersiz Diploma", error = "Öğrenci numarası zorunludur." });
            }

            var normalizedStudentIdentifier = studentIdentifier.Trim();

            var university = await dbContext.Universities.SingleOrDefaultAsync(
                item => item.Id == currentUser.UniversityId,
                cancellationToken);
            if (university is null)
            {
                return BadRequest(new { status = "Geçersiz Diploma", error = "Kullanıcı bir üniversiteye bağlı değil." });
            }

            var universityStudents = await userManager.GetUsersInRoleAsync(AppRoles.Student);
            var studentExists = universityStudents.Any(student =>
                student.UniversityId == university.Id &&
                string.Equals(student.StudentIdentifier, normalizedStudentIdentifier, StringComparison.OrdinalIgnoreCase));
            if (!studentExists)
            {
                return BadRequest(new { status = "Geçersiz Diploma", error = "Seçilen öğrenci bu üniversiteye kayıtlı değil." });
            }

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
            var verificationUrl = verificationLinkService.Build(HttpContext, pdfHash.HexHash);
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

            return Ok(new UploadDiplomaResponse(
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
                qrCodeDataUrl));
        }
        catch (PdfValidationException ex)
        {
            return BadRequest(new { status = "Geçersiz Diploma", error = ex.Message });
        }
        catch (DuplicateDiplomaException ex)
        {
            return Conflict(new { status = "Geçersiz Diploma", error = ex.Message });
        }
        catch (BlockchainConfigurationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "Konfigürasyon Hatası", error = ex.Message });
        }
        catch (BlockchainStateVerificationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { status = "Blockchain Doğrulama Hatası", error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { status = "Geçersiz Diploma", error = ex.Message });
        }
    }

    [Authorize(Roles = $"{AppRoles.Employer},{AppRoles.University},{AppRoles.Admin}")]
    [HttpPost("verify")]
    [RequestSizeLimit(PdfHashService.MaxMultipartBodySizeBytes)]
    public async Task<ActionResult<VerifyDiplomaResponse>> Verify(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var pdfHash = await pdfHashService.CreateHashAsync(file, cancellationToken);
            var verification = await blockchainService.VerifyAsync(pdfHash.Bytes32, cancellationToken);

            return Ok(await ToVerifyResponseAsync(pdfHash.HexHash, verification, "Geçersiz Diploma", cancellationToken));
        }
        catch (PdfValidationException ex)
        {
            return BadRequest(new { status = "Geçersiz Diploma", error = ex.Message });
        }
        catch (BlockchainConfigurationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "Konfigürasyon Hatası", error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("verification/{hash}")]
    public async Task<ActionResult<VerifyDiplomaResponse>> VerifyHash(string hash, CancellationToken cancellationToken)
    {
        try
        {
            var pdfHash = HexHashConverter.NormalizeDisplayHash(hash);
            var hashBytes = HexHashConverter.ToBytes32(pdfHash);
            var verification = await blockchainService.VerifyAsync(hashBytes, cancellationToken);

            return Ok(await ToVerifyResponseAsync(pdfHash, verification, "Blockchain Kaydı Bulunamadı", cancellationToken));
        }
        catch (PdfValidationException ex)
        {
            return BadRequest(new { status = "Geçersiz Diploma", error = ex.Message });
        }
        catch (BlockchainConfigurationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = "Konfigürasyon Hatası", error = ex.Message });
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
