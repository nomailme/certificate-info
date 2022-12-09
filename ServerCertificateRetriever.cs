using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MugenMvvmToolkit;

namespace CertificateViewerPlayground;

public class ServerCertificateRetriever
{
    public static async Task<X509Certificate2Collection> GetAsync(string address)
    {
        X509Certificate2Collection serverCertificates = new();
        var uri = new Uri(address);
        var policy = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust };


        var proxyUri = HttpClient.DefaultProxy.GetProxy(uri);

        using var client = proxyUri == null ? new TcpClient() : CreateProxied(proxyUri, uri);


        var sslStream = new SslStream(client.GetStream(), false);

        var sslClientAuthenticationOptions = new SslClientAuthenticationOptions
        {
            CertificateChainPolicy = policy,
            RemoteCertificateValidationCallback = (_, _, chain, _) =>
            {
                chain?.ChainElements.Select(x => x.Certificate).ForEach(x => serverCertificates.Add(new X509Certificate2(x.RawData)));
                return true;
            }
        };
        await sslStream.AuthenticateAsClientAsync(sslClientAuthenticationOptions);

        return serverCertificates;
    }

    private static TcpClient CreateProxied(Uri proxy, Uri destination)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        socket.Connect(proxy.Host, proxy.Port);

        var connectMessage = Encoding.UTF8.GetBytes($"CONNECT {destination.Host}:{destination.Port} HTTP/1.1{Environment.NewLine}{Environment.NewLine}");
        socket.Send(connectMessage);

        var receiveBuffer = new byte[1024];
        var received = socket.Receive(receiveBuffer);

        var response = Encoding.ASCII.GetString(receiveBuffer, 0, received);

        if (!response.Contains("200 OK") && !response.Contains("200 Connection established"))
        {
            throw new HttpRequestException($"Error connecting to proxy server {destination.Host}:{destination.Port}. Response: {response}");
        }

        return new TcpClient { Client = socket };
    }
}

