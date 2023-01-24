using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class PemImporter: ICertificateLoader<byte[]>, ICertificateTypeValidator<byte[]>
{
    public Task<ImportResult> ImportAsync(byte[] input, EmptyOptions? options=default)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            if (IsSupported(input) == false)
            {
                throw new WrongContentTypeException();
            }
            var data = Encoding.ASCII.GetString(input);
            certificateCollection.ImportFromPem(data);
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