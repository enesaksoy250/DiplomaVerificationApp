using System.Security.Claims;
using DiplomaVerificationApp.Models;

namespace DiplomaVerificationApp.Services;

public interface IDiplomaWorkflowService
{
    Task<UploadDiplomaResponse> UploadAsync(
        IFormFile file,
        string studentIdentifier,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken cancellationToken);

    Task<VerifyDiplomaResponse> VerifyFileAsync(IFormFile file, CancellationToken cancellationToken);

    Task<VerifyDiplomaResponse> VerifyHashAsync(string hash, CancellationToken cancellationToken);
}
