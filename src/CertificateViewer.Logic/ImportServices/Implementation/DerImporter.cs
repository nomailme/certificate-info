using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class DerImporter: IImporter<string>
{
    public async Task<ImportResult> ImportAsync(string input, params object[] parameters)
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
}