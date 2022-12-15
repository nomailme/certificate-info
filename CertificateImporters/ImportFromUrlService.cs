using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CertificateViewer.CertificateImporters;

public sealed class ImportFromUrlService
{
    public static async Task<OperationResult> OpenFromUrlAsync(string uri)
    {
        try
        {
            var certificates = await RetrieveServerCertificatesAsync(uri);
            return OperationResult.CreateSuccess(certificates.ToList(), CertificateType.Web);
        }
        catch (Exception e)
        {
            return OperationResult.CreateFail(e);
        }
    }

    private static async Task<X509Certificate2Collection> RetrieveServerCertificatesAsync(string address)
    {
        X509Certificate2Collection serverCertificates = new();
        var targetUri = new Uri(address);

        var policy = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust };
        var handler = new SocketsHttpHandler
        {
            UseProxy = HttpClient.DefaultProxy.IsBypassed(targetUri) == false,
            DefaultProxyCredentials = CredentialCache.DefaultNetworkCredentials,
            SslOptions = new SslClientAuthenticationOptions
            {
                CertificateChainPolicy = policy,
                RemoteCertificateValidationCallback = (_, _, chain, _) =>
                {
                    chain?.ChainElements.Select(x => x.Certificate).ToList().ForEach(x => serverCertificates.Add(new X509Certificate2(x.RawData)));
                    return true;
                }
            }
        };
        var httpClient = new HttpClient(handler);
        await httpClient.GetAsync(targetUri);

        return serverCertificates;
    }
}
