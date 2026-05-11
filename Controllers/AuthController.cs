using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaVerificationApp.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserSessionResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { error = "E-posta veya parola hatalı." });
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = "E-posta veya parola hatalı." });
        }

        return Ok(await CreateSessionResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public async Task<ActionResult<UserSessionResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Ok(new UserSessionResponse(false, null, [], null, null));
        }

        return Ok(await CreateSessionResponseAsync(user));
    }

    private async Task<UserSessionResponse> CreateSessionResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserSessionResponse(true, user.Email, roles.ToArray(), user.UniversityId, user.StudentIdentifier);
    }
}
