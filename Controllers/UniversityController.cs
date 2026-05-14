using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Models;
using DiplomaVerificationApp.Security;
using DiplomaVerificationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.University)]
[Route("university")]
public sealed class UniversityController(
    UserManager<ApplicationUser> userManager,
    IDiplomaRecordRepository diplomaRecordRepository) : ControllerBase
{
    [HttpGet("students")]
    public async Task<ActionResult<IReadOnlyList<StudentAccountResponse>>> ListStudents()
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (string.IsNullOrWhiteSpace(currentUser?.UniversityId))
        {
            return BadRequest(new { error = "Üniversite hesabı bir üniversite ile ilişkilendirilmemiş." });
        }

        var students = (await userManager.GetUsersInRoleAsync(AppRoles.Student))
            .Where(student => student.UniversityId == currentUser.UniversityId)
            .Where(student => !string.IsNullOrWhiteSpace(student.StudentIdentifier))
            .OrderBy(student => student.StudentIdentifier)
            .ThenBy(student => student.Email)
            .ToArray();
        var response = new List<StudentAccountResponse>();

        foreach (var student in students)
        {
            var records = await diplomaRecordRepository.GetByStudentIdentifierAsync(
                student.StudentIdentifier!,
                HttpContext.RequestAborted);
            var universityRecords = records
                .Where(record => record.UniversityId == currentUser.UniversityId)
                .ToArray();

            response.Add(new StudentAccountResponse(
                student.Id,
                student.Email,
                student.StudentIdentifier!,
                universityRecords.Length,
                universityRecords.Length > 0,
                universityRecords
                    .OrderByDescending(record => record.RegisteredAtUtc)
                    .FirstOrDefault()?
                    .RegisteredAtUtc));
        }

        return Ok(response);
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(CreateStudentRequest request)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (string.IsNullOrWhiteSpace(currentUser?.UniversityId))
        {
            return BadRequest(new { error = "Üniversite hesabı bir üniversite ile ilişkilendirilmemiş." });
        }

        if (string.IsNullOrWhiteSpace(request.StudentIdentifier))
        {
            return BadRequest(new { error = "Öğrenci numarası zorunludur." });
        }

        var student = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            UniversityId = currentUser.UniversityId,
            StudentIdentifier = request.StudentIdentifier.Trim()
        };

        var result = await userManager.CreateAsync(student, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = string.Join("; ", result.Errors.Select(error => error.Description)) });
        }

        await userManager.AddToRoleAsync(student, AppRoles.Student);
        return Ok(new
        {
            student.Id,
            student.Email,
            Role = AppRoles.Student,
            student.UniversityId,
            student.StudentIdentifier
        });
    }
}
