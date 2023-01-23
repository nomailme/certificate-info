using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices;

public static class CertificateHelper
{
    public static async ValueTask<CertificateType> CheckAsync(string filename)
    {
        var contentType = X509Certificate2.GetCertContentType(filename);
        if (contentType == X509ContentType.Pfx)
        {
            return CertificateType.Pfx;
        }
        if (contentType == X509ContentType.Cert)
        {
            if (await IsPemCertificate(filename))
            {
                return CertificateType.Pem;
            }
            if (await IsDerCertificate(filename))
            {
                return CertificateType.Der;
            }
        }
        return CertificateType.Unknown;
    }

    public static async ValueTask<bool> IsPemCertificate(string filename)
    {
        var magicBytes = "-----BEGIN CERTIFICATE-----"u8.ToArray();
        await using var stream = File.OpenRead(filename);
        return await ValidateUsingMagicBytes(stream, magicBytes);
    }


    public static async ValueTask<bool> IsDerCertificate(string filename)
    {
        var magicBytes = new byte[] { 0x30, 0x82 };
        await using var stream = File.OpenRead(filename);
        return await ValidateUsingMagicBytes(stream, magicBytes);
    }

    public static async ValueTask<bool> IsDerCertificate(byte[] rawData)
    {
        var magicBytes = new byte[] { 0x30, 0x82 };
        await using var stream = new MemoryStream(rawData);
        return await ValidateUsingMagicBytes(stream, magicBytes);
    }

    private static async ValueTask<bool> ValidateUsingMagicBytes(Stream stream, IReadOnlyCollection<byte> magicBytes)
    {
        var buffer = new byte[magicBytes.Count];
        await stream.ReadExactlyAsync(buffer, 0, magicBytes.Count);
        return buffer.SequenceEqual(magicBytes);
    }
}