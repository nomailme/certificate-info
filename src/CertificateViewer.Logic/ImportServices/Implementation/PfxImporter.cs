using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class PfxImporter: ICertificateLoader<byte[], PfxImporter.PfxLoaderOptions>, ICertificateTypeValidator<byte[]>, ICertificateFileImporter
{
    public class PfxLoaderOptions: ICertificateLoaderOptions
    {
        public string Password { get; set; } = string.Empty;
    }

    public Task<ImportResult> ImportAsync(byte[] input, PfxLoaderOptions? options)
    {
        if (options is not { } pfxOptions)
        {
            throw new ArgumentException("Use PfxLoader options as a parameter", nameof(options));
        }
        try
        {
            var contentType = X509Certificate2.GetCertContentType(input);
            if (contentType != X509ContentType.Pfx)
            {
                throw new WrongContentTypeException();
            }
            var certificateCollection = X509CertificateLoader.LoadPkcs12Collection(input, pfxOptions.Password);
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

    public Task<ImportResult> ImportAsync(byte[] data, string password)
    {
        // The file-importer contract passes the password directly; wrap it in the options
        // the options-based overload expects.
        return ImportAsync(data, new PfxLoaderOptions { Password = password });
    }

    public bool IsSupported(byte[] input)
    {
        var type = GetCertContentTypeSafe(input);
        return type == X509ContentType.Pfx;
    }

    /// <summary>
    ///     <see cref="X509Certificate2.GetCertContentType" /> throws
    ///     <see cref="CryptographicException" /> for byte sequences that are not a recognized
    ///     certificate encoding (e.g. arbitrary user input). A type probe must never throw —
    ///     an unsupported input simply means "not PFX".
    /// </summary>
    private static X509ContentType GetCertContentTypeSafe(byte[] input)
    {
        try
        {
            return X509Certificate2.GetCertContentType(input);
        }
        catch (CryptographicException)
        {
            return X509ContentType.Unknown;
        }
    }
}
