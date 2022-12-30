using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class PemImporter: IImporter<string>
{
    public async Task<ImportResult> ImportAsync(string input, params object[] parameters)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            if (await CertificateHelper.IsPemCertificate(input) == false)
            {
                throw new WrongContentTypeException();
            }

            certificateCollection.ImportFromPemFile(input);
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