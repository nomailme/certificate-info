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
        using (var handler = BuildHandler(targetUri, serverCertificates))
        using (var httpClient = new HttpClient(handler))
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            await httpClient.GetAsync(targetUri, cts.Token);
        }

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
                // Deliberately permissive: this tool inspects invalid/self-signed certificates; trust is decided later by chain validation, not here.
                RemoteCertificateValidationCallback = (_, _, chain, _) =>
                {
                    var chainElements = chain?.ChainElements.Select(x => x.Certificate).ToList();
                    if (chainElements != null)
                    {
                        foreach (var x in chainElements)
                            collection.Add(new X509Certificate2(x.RawData));
                    }
                    return true;
                }
            }
        };
        return handler;
    }
}