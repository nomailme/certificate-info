using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MugenMvvmToolkit;

namespace CertificateViewerPlayground;

public class ServerCertificateRetriever
{
    public async Task<X509Certificate2Collection> GetAsync(string address)
    {
        X509Certificate2Collection serverCertificates =new ();
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, arg3, _) =>
        {
            serverCertificates =new ();
            arg3?.ChainElements.Select(x => x.Certificate).ForEach(x => serverCertificates.Add(new X509Certificate2(x.RawData)));
            return true;
        };
        var client = new HttpClient(handler);
        var result = await client.GetAsync(address);
        return serverCertificates;
    }
}
