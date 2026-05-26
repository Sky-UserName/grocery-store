using QRCoder;

namespace GroceryStoreSystem.Services;

public sealed class QrCodeService
{
    public byte[] CreatePng(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(12);
    }
}
