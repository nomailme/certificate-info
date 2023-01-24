using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic.ImportServices.Implementation;

public class RemoteServerImporter: ICertificateLoader<string>
{
    public async Task<ImportResult> ImportAsync(string input, EmptyOptions? options = default)
    {
        try
        {
            var certificates = await RetrieveServerCertificatesAsync(input);
            return ImportResult.CreateSuccess(certificates.ToList());
        }
        catch (Exception e)
        {
            return ImportResult.CreateFail(e);
        }
    }
    private static async Task<X509Certificate2Collection> RetrieveServerCertificatesAsync(string address)
    {
        X509Certificate2Collection serverCertificates = new();
        var targetUri = new Uri(address);
        var handler = BuildHandler(targetUri, serverCertificates);
        var httpClient = new HttpClient(handler);
        await httpClient.GetAsync(targetUri);

        return serverCertificates;
    }

    private static SocketsHttpHandler BuildHandler(Uri targetUri, X509Certificate2Collection collection)
    {
        var policy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust
        };
        var handler = new SocketsHttpHandler
        {
            UseProxy = HttpClient.DefaultProxy.IsBypassed(targetUri) == false,
            DefaultProxyCredentials = CredentialCache.DefaultNetworkCredentials,
            SslOptions = new SslClientAuthenticationOptions
            {
                CertificateChainPolicy = policy,
                RemoteCertificateValidationCallback = (_, _, chain, _) =>
                {
                    chain?.ChainElements
                        .Select(x => x.Certificate)
                        .ToList()
                        .ForEach(x => collection.Add(new X509Certificate2(x.RawData)));
                    return true;
                }
            }
        };
        return handler;
    }
}