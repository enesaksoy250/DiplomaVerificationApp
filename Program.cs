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

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services);

app.Run();
