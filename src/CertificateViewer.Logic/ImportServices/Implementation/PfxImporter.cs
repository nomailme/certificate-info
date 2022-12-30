using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class PfxImporter: IImporter<string>
{
    public Task<ImportResult> ImportAsync(string input, params object[] parameters)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            var contentType = X509Certificate2.GetCertContentType(input);
            if (contentType != X509ContentType.Pkcs12)
            {
                throw new WrongContentTypeException();
            }
            if (parameters[0] is not string password)
            {
                throw new ArgumentException("Provide password for a PFX file");
            }
            certificateCollection.Import(input, password);
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
}