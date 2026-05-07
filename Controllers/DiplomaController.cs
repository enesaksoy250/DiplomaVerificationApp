using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Route("")]
public sealed class DiplomaController(
    IPdfHashService pdfHashService,
    IDiplomaBlockchainService blockchainService,
    IVerificationLinkService verificationLinkService,
    IQrCodeService qrCodeService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(PdfHashService.MaxMultipartBodySizeBytes)]
    public async Task<ActionResult<UploadDiplomaResponse>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var pdfHash = await pdfHashService.CreateHashAsync(file, cancellationToken);
            var blockchainResult = await blockchainService.RegisterAsync(pdfHash.Bytes32, cancellationToken);
            var verificationUrl = verificationLinkService.Build(HttpContext, pdfHash.HexHash);
            var qrCodeDataUrl = qrCodeService.CreateDataUrl(verificationUrl);

            return Ok(new UploadDiplomaResponse(
                "Geçerli Diploma",
                pdfHash.HexHash,
                blockchainResult.TransactionHash,
                blockchainResult.Timestamp,
                blockchainResult.RegisteredAtUtc,
                blockchainResult.Network,
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
    }

    [HttpPost("verify")]
    [RequestSizeLimit(PdfHashService.MaxMultipartBodySizeBytes)]
    public async Task<ActionResult<VerifyDiplomaResponse>> Verify(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var pdfHash = await pdfHashService.CreateHashAsync(file, cancellationToken);
            var verification = await blockchainService.VerifyAsync(pdfHash.Bytes32, cancellationToken);

            return Ok(ToVerifyResponse(pdfHash.HexHash, verification, "Geçersiz Diploma"));
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

    [HttpGet("verification/{hash}")]
    public async Task<ActionResult<VerifyDiplomaResponse>> VerifyHash(string hash, CancellationToken cancellationToken)
    {
        try
        {
            var pdfHash = HexHashConverter.NormalizeDisplayHash(hash);
            var hashBytes = HexHashConverter.ToBytes32(pdfHash);
            var verification = await blockchainService.VerifyAsync(hashBytes, cancellationToken);

            return Ok(ToVerifyResponse(pdfHash, verification, "Blockchain Kaydı Bulunamadı"));
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

    private static VerifyDiplomaResponse ToVerifyResponse(
        string pdfHash,
        BlockchainVerificationResult verification,
        string missingStatus)
    {
        var status = verification.Exists
            ? "Geçerli Diploma"
            : missingStatus;

        return new VerifyDiplomaResponse(
            status,
            pdfHash,
            verification.Exists,
            verification.Exists ? verification.Timestamp : null,
            verification.RegisteredAtUtc,
            verification.TransactionHash,
            verification.Network);
    }
}
