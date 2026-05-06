using DiplomaVerificationApp.Options;
using Microsoft.Extensions.Options;

namespace DiplomaVerificationApp.Services;

public sealed class VerificationLinkService(IOptions<BlockchainOptions> options) : IVerificationLinkService
{
    private readonly BlockchainOptions _options = options.Value;

    public string Build(HttpContext httpContext, string pdfHash)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.VerificationBaseUrl)
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : _options.VerificationBaseUrl.TrimEnd('/');

        return $"{baseUrl}/verify.html?hash={Uri.EscapeDataString(pdfHash)}";
    }
}
