using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class DerImporter: ICertificateLoader<byte[]>, ICertificateTypeValidator<byte[]>, ICertificateFileImporter
{
    public async Task<ImportResult> ImportAsync(byte[] input, EmptyOptions? options=default)
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

    public Task<ImportResult> ImportAsync(byte[] data, string password)
    {
        // A DER certificate needs no password; the parameter is accepted for the
        // file-importer contract and ignored here.
        return ImportAsync(data, (EmptyOptions?)null);
    }

    // Uses the same robust DER-structure check as the rest of the pipeline instead of the
    // legacy {0x30, 0x82} magic-byte prefix, so strategy selection agrees with detection.
    public bool IsSupported(byte[] rawData)
    {
        return rawData.Length >= 2 && rawData[0] == 0x30 && CertificateHelper.IsDerCertificate(rawData).Result;
    }
}
