using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class PemImporter: CertificateLoader<byte[]>, ICertificateTypeValidator<byte[]>
{
    protected override Task<ImportResult> ImportCore(byte[] input, EmptyOptions? options)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            if (IsSupported(input) == false)
            {
                throw new WrongContentTypeException();
            }

            certificateCollection.Import(input);
            if (certificateCollection.Any() == false)
            {
                throw new ArgumentException("No certificates found in file");
            }
            return Task.FromResult(ImportResult.CreateSuccess(certificateCollection.ToList()));
        }
        catch (Exception e)
        {
            return Task.FromResult(ImportResult.CreateFail(e));
        }
    }
    
    public bool IsSupported(byte[] rawData)
    {
        var magicBytes = "-----BEGIN CERTIFICATE-----"u8.ToArray();
        return MagicByteHelper.Match(rawData, magicBytes);
    }
}