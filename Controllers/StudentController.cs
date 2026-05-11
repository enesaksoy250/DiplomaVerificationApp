using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Security;
using DiplomaVerificationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Student)]
[Route("student")]
public sealed class StudentController(
    UserManager<ApplicationUser> userManager,
    IDiplomaRecordRepository diplomaRecordRepository) : ControllerBase
{
    [HttpGet("diplomas")]
    public async Task<ActionResult<IReadOnlyList<StudentDiplomaResponse>>> GetDiplomas(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (string.IsNullOrWhiteSpace(user?.StudentIdentifier))
        {
            return Ok(Array.Empty<StudentDiplomaResponse>());
        }

        var records = await diplomaRecordRepository.GetByStudentIdentifierAsync(user.StudentIdentifier, cancellationToken);
        return Ok(records.Select(record => new StudentDiplomaResponse(
            record.PdfHash,
            record.TransactionHash,
            record.UniversityName,
            record.SignatureHash,
            record.RegisteredAtUtc)).ToArray());
    }
}
