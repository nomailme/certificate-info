using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class PfxImporter: ICertificateLoader<byte[], PfxImporter.PfxLoaderOptions>, ICertificateTypeValidator<byte[]>
{
    public class PfxLoaderOptions: ICertificateLoaderOptions
    {
        public string Password { get; set; } = string.Empty;
    }

    public Task<ImportResult> ImportAsync(byte[] input, PfxLoaderOptions options)
    {
        var certificateCollection = new X509Certificate2Collection();
        if (options is not { } pfxOptions)
        {
            throw new ArgumentException("Use PfxLoader options as a parameter", nameof(options));
        }
        try
        {
            var contentType = X509Certificate2.GetCertContentType(input);
            if (contentType != X509ContentType.Pkcs12)
            {
                throw new WrongContentTypeException();
            }
            certificateCollection.Import(input, pfxOptions.Password);
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

    public bool IsSupported(byte[] input)
    {
        var type = X509Certificate2.GetCertContentType(input);
        return type == X509ContentType.Pfx;
    }
}