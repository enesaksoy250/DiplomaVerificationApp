using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Security;
using DiplomaVerificationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Route("")]
public sealed class DiplomaController(IDiplomaWorkflowService diplomaWorkflowService) : ControllerBase
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
            return Ok(await diplomaWorkflowService.UploadAsync(
                file,
                studentIdentifier,
                User,
                HttpContext,
                cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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
            return Ok(await diplomaWorkflowService.VerifyFileAsync(file, cancellationToken));
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
            return Ok(await diplomaWorkflowService.VerifyHashAsync(hash, cancellationToken));
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
}
