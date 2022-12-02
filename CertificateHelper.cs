using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace CertificateViewerPlayground;

public class CertificateHelper
{
    private readonly ILogger _logger = Log.ForContext<CertificateHelper>();

    public X509Certificate2 Open(string filePath)
    {
        var pemResult = LoadPemChain(filePath);
        if (pemResult.Success)
        {
            return pemResult.Certificates.First();
        }
        var derResult = LoadDerCertificate(filePath);
        if (derResult.Success)
        {
            return derResult.Certificates.First();
        }
        throw new ArgumentException("Unable to load certificate file");
    }

    public (X509Certificate2Collection Certificates, CertificateType Type) LoadCertificate(string filename)
    {
        var pemResult = LoadPemChain(filename);
        if (pemResult.Success)
        {
            return (pemResult.Certificates, CertificateType.Pem);
        }
        var derResult = LoadDerCertificate(filename);
        if (derResult.Success)
        {
            return (derResult.Certificates, CertificateType.Der);
        }
        throw new ArgumentException("Unable to load certificate file");
    }

    private (X509Certificate2Collection Certificates, bool Success) LoadPemChain(string filename)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            certificateCollection.ImportFromPemFile(filename);
            if (certificateCollection.Count == 0)
            {
                return (certificateCollection, false);
            }
            return (certificateCollection, true);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error loading certificate as PEM");
            return (certificateCollection, false);
        }

    }

    private (X509Certificate2Collection Certificates, bool Success) LoadDerCertificate(string filename)
    {
        var certificateCollection = new X509Certificate2Collection();
        try
        {
            certificateCollection.Import(filename);
            return (certificateCollection, true);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error loading certificate as PEM");
            return (certificateCollection, false);
        }
    }
}
