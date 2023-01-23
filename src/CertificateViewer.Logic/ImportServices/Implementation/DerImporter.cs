using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class DerImporter: CertificateLoader<byte[]>, ICertificateTypeValidator<byte[]>
{
    protected override async Task<ImportResult> ImportCore(byte[] input, EmptyOptions? options)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            if (await CertificateHelper.IsDerCertificate(input) == false)
            {
                throw new WrongContentTypeException();
            }

            certificateCollection.Import(input);
            if (certificateCollection.Any() == false)
            {
                throw new ArgumentException("No certificates found in file");
            }
            return ImportResult.CreateSuccess(certificateCollection.ToList());
        }
        catch (Exception e)
        {
            return ImportResult.CreateFail(e);
        }
    }
    
    public bool IsSupported(byte[] rawData)
    {
        var magicBytes = new byte[] { 0x30, 0x82 };
        return MagicByteHelper.Match(rawData, magicBytes);
    }
}