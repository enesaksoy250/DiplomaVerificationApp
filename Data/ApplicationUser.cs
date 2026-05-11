using Microsoft.AspNetCore.Identity;

namespace DiplomaVerificationApp.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string? UniversityId { get; set; }

    public string? StudentIdentifier { get; set; }
}
