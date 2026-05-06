namespace DiplomaVerificationApp.Services;

public interface IQrCodeService
{
    string CreateDataUrl(string value);
}
