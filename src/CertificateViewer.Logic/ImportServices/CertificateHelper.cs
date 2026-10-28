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
        await using var stream = File.OpenRead(filename);
        var header = new byte[4];
        var read = 0;
        while (read < header.Length)
        {
            var count = await stream.ReadAsync(header, read, header.Length - read);
            if (count == 0)
            {
                break;
            }
            read += count;
        }
        return await IsDerCertificate(0 == read ? Array.Empty<byte>() : header.Take(read).ToArray());
    }

    public static ValueTask<bool> IsDerCertificate(byte[] rawData)
    {
        return ValueTask.FromResult(CheckDerStructure(rawData));
    }

    public static async ValueTask<CertificateType> CheckAsync(byte[] rawData)
    {
        var contentType = X509Certificate2.GetCertContentType(rawData);
        if (contentType == X509ContentType.Pfx)
        {
            return CertificateType.Pfx;
        }
        if (contentType == X509ContentType.Cert)
        {
            if (IsPemCertificate(rawData))
            {
                return CertificateType.Pem;
            }
            if (await IsDerCertificate(rawData))
            {
                return CertificateType.Der;
            }
        }
        return CertificateType.Unknown;
    }

    public static bool IsPemCertificate(byte[] rawData)
    {
        var magicBytes = "-----BEGIN CERTIFICATE-----"u8.ToArray();
        return rawData.Length >= magicBytes.Length && rawData.AsSpan(0, magicBytes.Length).SequenceEqual(magicBytes);
    }

    private static bool CheckDerStructure(byte[] data)
    {
        if (data.Length < 2)
        {
            return false;
        }
        if (data[0] != 0x30) // SEQUENCE
        {
            return false;
        }
        switch (data[1])
        {
            case 0x80:
                // Indefinite length is not valid DER.
                return false;
            case 0x81:
                // Long form: one length byte follows.
                return data.Length >= 3;
            case 0x82:
                // Long form: two length bytes follow.
                return data.Length >= 4;
            case var n when n is >= 0x00 and <= 0x7F:
                // Short form: the two header bytes suffice.
                return true;
            default:
                return false;
        }
    }

    private static async ValueTask<bool> ValidateUsingMagicBytes(Stream stream, IReadOnlyCollection<byte> magicBytes)
    {
        var buffer = new byte[magicBytes.Count];
        await stream.ReadExactlyAsync(buffer, 0, magicBytes.Count);
        return buffer.SequenceEqual(magicBytes);
    }
}
