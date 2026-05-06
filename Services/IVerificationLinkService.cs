namespace DiplomaVerificationApp.Services;

public interface IVerificationLinkService
{
    string Build(HttpContext httpContext, string pdfHash);
}
