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
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IPdfHashService, DiplomaVerificationApp.Services.PdfHashService>();
builder.Services.AddSingleton<DiplomaVerificationApp.Services.IDiplomaRecordRepository, DiplomaVerificationApp.Services.SqliteDiplomaRecordRepository>();
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

app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
