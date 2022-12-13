using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace CertificateViewer.CertificateImporters;

public class ImportFromFileHelper
{
    private readonly ILogger logger = Log.ForContext<ImportFromFileHelper>();


    public OperationResult LoadCertificate(string filename)
    {
        var pemResult = LoadPemChain(filename);
        if (pemResult.Success)
        {
            return OperationResult.CreateSuccess(pemResult.Certificates, CertificateType.Pem);
        }
        var derResult = LoadDerCertificate(filename);
        if (derResult.Success)
        {
            return OperationResult.CreateSuccess(derResult.Certificates, CertificateType.Der);
        }
        return OperationResult.CreateFail(new ArgumentException("Unable to load certificate file"));
    }

    private (List<X509Certificate2> Certificates, bool Success) LoadPemChain(string filename)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            certificateCollection.ImportFromPemFile(filename);
            if (certificateCollection.Any())
            {
                return (certificateCollection.ToList(), true);
            }

        }
        catch (Exception e)
        {
            logger.Error(e, "Error loading certificate as PEM");
        }
        return (certificateCollection.ToList(), false);

    }

    private (List<X509Certificate2> Certificates, bool Success) LoadDerCertificate(string filename)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            certificateCollection.Import(filename);
            return (certificateCollection.ToList(), true);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error loading certificate as PEM");
            return (certificateCollection.ToList(), false);
        }
    }
}
