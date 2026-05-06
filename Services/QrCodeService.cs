using QRCoder;

namespace DiplomaVerificationApp.Services;

public sealed class QrCodeService : IQrCodeService
{
    public string CreateDataUrl(string value)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(16);

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
