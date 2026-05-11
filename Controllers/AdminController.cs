using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Security;
using DiplomaVerificationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("admin")]
public sealed class AdminController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IUniversityKeyService universityKeyService) : ControllerBase
{
    [HttpPost("universities")]
    public async Task<ActionResult<UniversityResponse>> CreateUniversity(
        CreateUniversityRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Üniversite adı zorunludur." });
        }

        var university = new University { Name = request.Name.Trim() };
        dbContext.Universities.Add(university);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(university));
    }

    [HttpGet("universities")]
    public async Task<ActionResult<IReadOnlyList<UniversityResponse>>> ListUniversities(CancellationToken cancellationToken)
    {
        var universities = await dbContext.Universities
            .OrderBy(university => university.Name)
            .ToListAsync(cancellationToken);

        return Ok(universities.Select(ToResponse).ToArray());
    }

    [HttpPost("universities/{id}/keys")]
    public async Task<ActionResult<UniversityResponse>> GenerateKeys(string id, CancellationToken cancellationToken)
    {
        await universityKeyService.GenerateKeyPairAsync(id, cancellationToken);

        var university = await dbContext.Universities.SingleAsync(item => item.Id == id, cancellationToken);
        return Ok(ToResponse(university));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        if (!AppRoles.All.Contains(request.Role))
        {
            return BadRequest(new { error = "Geçersiz rol." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            UniversityId = request.UniversityId,
            StudentIdentifier = request.StudentIdentifier
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = string.Join("; ", result.Errors.Select(error => error.Description)) });
        }

        await userManager.AddToRoleAsync(user, request.Role);
        return Ok(new { user.Id, user.Email, request.Role, user.UniversityId, user.StudentIdentifier });
    }

    private static UniversityResponse ToResponse(University university)
    {
        return new UniversityResponse(
            university.Id,
            university.Name,
            !string.IsNullOrWhiteSpace(university.PublicKeyPem) && !string.IsNullOrWhiteSpace(university.ProtectedPrivateKeyPem),
            university.CreatedAtUtc,
            university.KeyCreatedAtUtc);
    }
}
