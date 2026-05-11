namespace DiplomaVerificationApp.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string University = "University";
    public const string Student = "Student";
    public const string Employer = "Employer";

    public static readonly string[] All = [Admin, University, Student, Employer];
}
