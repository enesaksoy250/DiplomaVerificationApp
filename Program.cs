using DiplomaVerificationApp.Data;
using DiplomaVerificationApp.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = DiplomaVerificationApp.Services.PdfHashService.MaxMultipartBodySizeBytes;
});
builder.Services.Configure<DiplomaVerificationApp.Options.BlockchainOptions>(
    builder.Configuration.GetSection("Blockchain"));
builder.Services.Configure<DiplomaVerificationApp.Options.DiplomaRecordStorageOptions>(
    builder.Configuration.GetSection("DiplomaRecordStorage"));
builder.Services.Configure<DiplomaVerificationApp.Options.IdentityStorageOptions>(
    builder.Configuration.GetSection("IdentityStorage"));
builder.Services.Configure<DiplomaVerificationApp.Options.AdminSeedOptions>(
    builder.Configuration.GetSection("SeedAdmin"));
builder.Services.Configure<DiplomaVerificationApp.Options.DiplomaFileStorageOptions>(
    builder.Configuration.GetSection("DiplomaFileStorage"));
var identityConnectionString = builder.Configuration
    .GetSection("IdentityStorage")
    .Get<IdentityStorageOptions>()?
    .ConnectionString;
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(string.IsNullOrWhiteSpace(identityConnectionString)
        ? "Data Source=identity.db"
        : identityConnectionString));
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "DiplomaVerification.Auth";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IPdfHashService, DiplomaVerificationApp.Services.PdfHashService>();
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IDiplomaRecordRepository, DiplomaVerificationApp.Services.SqliteDiplomaRecordRepository>();
builder.Services.AddScoped<DiplomaVerificationApp.Services.IUniversityKeyService, DiplomaVerificationApp.Services.UniversityKeyService>();
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IDiplomaFileStorageService, DiplomaVerificationApp.Services.DiplomaFileStorageService>();
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IDiplomaBlockchainService, DiplomaVerificationApp.Services.DiplomaBlockchainService>();
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IVerificationLinkService, DiplomaVerificationApp.Services.VerificationLinkService>();
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IQrCodeService, DiplomaVerificationApp.Services.QrCodeService>();
builder.Services.AddScoped<DiplomaVerificationApp.Services.IDiplomaWorkflowService, DiplomaVerificationApp.Services.DiplomaWorkflowService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var protectedPages = new Dictionary<PathString, string[]>
    {
        ["/index.html"] = [DiplomaVerificationApp.Security.AppRoles.University],
        ["/verify.html"] =
        [
            DiplomaVerificationApp.Security.AppRoles.Admin,
            DiplomaVerificationApp.Security.AppRoles.University,
            DiplomaVerificationApp.Security.AppRoles.Employer
        ],
        ["/admin.html"] = [DiplomaVerificationApp.Security.AppRoles.Admin],
        ["/student.html"] = [DiplomaVerificationApp.Security.AppRoles.Student],
        ["/university-students.html"] = [DiplomaVerificationApp.Security.AppRoles.University]
    };

    if (context.Request.Path == "/")
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.Redirect("/login.html");
            return;
        }

        context.Response.Redirect(context.User.IsInRole(DiplomaVerificationApp.Security.AppRoles.Student)
            ? "/student.html"
            : "/verify.html");
        return;
    }

    if (protectedPages.TryGetValue(context.Request.Path, out var allowedRoles))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/login.html?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        if (!allowedRoles.Any(context.User.IsInRole))
        {
            context.Response.Redirect("/");
            return;
        }
    }

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();
