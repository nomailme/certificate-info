using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CertificateViewer.Logic.ImportServices;
using CertificateViewer.Logic.ImportServices.Implementation;

namespace CertificateViewer.Tests.ImportServices;

public class RemoteServerImporterTests
{
    private const string CertPemBase64 =
        "LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tCk1JSURDVENDQWZHZ0F3SUJBZ0lVV0ZWalc3YVBCNmNua2VCcGtaNlZ5OS9yNDE4d0RRWUpLb1pJaHZjTkFRRUwKQlFBd0ZERVNNQkFHQTFVRUF3d0piRzlqWVd4b2IzTjBNQjRYRFRJMk1UQXlOekUzTVRJMU0xb1hEVEkyTVRBeQpPVEUzTVRJMU0xb3dGREVTTUJBR0ExVUVBd3dKYkc5allXeG9iM04wTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGCkFBT0NBUThBTUlJQkNnS0NBUUVBcEpKUWk4RlZHNzJzeDdIb0xNcmNkd2YzWEN5NGN6QzlKZnJJd0V2V05zbmMKTHhXT0RmRTAwcGZVNXZ3ZmlPc0ViTXE0Znl6TmhTTXQzWXpQb0x0Qmt3cWFDZUY2UlBnd21CWjY0b0JCbS95awpmbnVQd2IvZzJTaU10d3dzRmNtWlphMktlYkhBMU5ZZ2prOFNkTUF3NEpqRmNCWE9rNS81QUhpRnZrMHhRZHVaCjZueWtoeFVVeTB5S2xEa0hmYTVNMUNHanI1bXFTY1BnaXhKbDJXUFd0Y1I4MnJiK1BqWWRNMVMxWnEyVGNTR0YKMFpEbVJmd1cvdVRkbExJTnRMZlBzOXh6WFduOVFqWUtjUHJCS1pBeE84QUF4d3o5Y29SNkg4MVNpUEFKUTF2ZQpwZzQweitJWFBXc2JMbHoyNGhQMmJHWllEYkU5MTFQYlptNU15S0JUQXdJREFRQUJvMU13VVRBZEJnTlZIUTRFCkZnUVVBbldXNzhZTkYyK1p0WHU0L0tRM0h5U0JqOW93SHdZRFZSMGpCQmd3Rm9BVUFuV1c3OFlORjIrWnRYdTQKL0tRM0h5U0JqOW93RHdZRFZSMFRBUUgvQkFVd0F3RUIvekFOQmdrcWhraUc5dzBCQVFzRkFBT0NBUUVBb1dGeQowdEFLTS9VVTUxbWRPZWw5T21PaFZGWUxzek1RR3lHV0FCT1NMZEhmWERXQjFNQVRJWjlKcmlqV2REUWtSMk9kCnNWTlBSV2N1cVhSeGxlaG4wWVc1V2lpbEpOL1djRU1EM09NUytTSDNualZ5LzBaczUvRThTZzVXeDVZUWp4bUoKcDFIa2Z2TUxtYk5xcVdncjV2Y1g1TWU5VkxMWnlXY3BpanhNSlJJNnVPalltMTBqOXNKSlowVzZjb05jVndmYQpQWTh3QTJ2ZWFIdmFjQ3FCQy9SdFArWklOOFNaWi8vVDFEVHQ2SVVRYlhBTmhvM0kyTmdVY3VaK0FGUGFBUTEvCjdnNmFqSnVXeGxqaTNsMi9BT1BaeDNNaUlodWNBaXBCTW03eXp5U05iWHlKRkMvbkRaeS9reHE5aFBBT3dKQlUKY2Rlbk9vaDFhZkFlbEFucGlnPT0KLS0tLS1FTkQgQ0VSVElGSUNBVEUtLS0tLQo=";

    private const string KeyPemBase64 =
        "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSUV2Z0lCQURBTkJna3Foa2lHOXcwQkFRRUZBQVNDQktnd2dnU2tBZ0VBQW9JQkFRQ2trbENMd1ZVYnZhekgKc2Vnc3l0eDNCL2RjTExoek1MMGwrc2pBUzlZMnlkd3ZGWTROOFRUU2w5VG0vQitJNndSc3lyaC9MTTJGSXkzZApqTStndTBHVENwb0o0WHBFK0RDWUZucmlnRUdiL0tSK2U0L0J2K0RaS0l5M0RDd1Z5WmxscllwNXNjRFUxaUNPClR4SjB3RERnbU1Wd0ZjNlRuL2tBZUlXK1RURkIyNW5xZktTSEZSVExUSXFVT1FkOXJrelVJYU92bWFwSncrQ0wKRW1YWlk5YTF4SHphdHY0K05oMHpWTFZtclpOeElZWFJrT1pGL0JiKzVOMlVzZzIwdDgrejNITmRhZjFDTmdwdworc0Vwa0RFN3dBREhEUDF5aEhvZnpWS0k4QWxEVzk2bURqVFA0aGM5YXhzdVhQYmlFL1pzWmxnTnNUM1hVOXRtCmJreklvRk1EQWdNQkFBRUNnZ0VBQWVRcjZIZENmdmVla1RXZENVcSszdC8rRCt2MFVMVS9oSklOd0R2KzRidjUKMFB3S21RN0JGZEhUTmd1eFljQkZydW00MWhQOGtWVDJxczc2b2QrL3RnSVBXWGE0WjREWFdpQVpXRzlkNXY0dgp5OE9vR0I0WU81YW80S3BUVUFySmt3UVJZb0JRU3RjM3pPV01Fa0JxaExlWGFVZ3U0QlBIWjZIUVNRQVh4Vk1pCmlYQ0NZMXdSWlRUQkhNOFRGMWluZnhjYWM0bXVsY1B3eXhpYTl0Vkd6eFE3K01WYjZrcWJqWDdkK2FjbGp3aVUKMUNSVEVqK0w3cHBpb3dQb1dzeGRuR2VUZjExMXd3UjIzVVFIMXE1YjlEZzUwUENJQXI2cUYzL2JuTUhSWm10NgovaVZnTTcrZTlvczlxbU85U2VLWm5MK3VmVi9VdFFCZmt0cGJHQ242bFFLQmdRRE5qSEJDY01VbUoyWFNwcUFBCjBGREZJVzU5YUxzeXRmOGo1V0VWQXFQL0JkMndqMEErWjVtMDVqZkVZREFBOGVsUHZhRnBBVGtmRXR1RHRXWVUKTkoxZ1VVd25pUU5IOTN6dXRCdHlMd2d5SzJaMFdVbnVJSFBicUQwZDFvV2JXeVo0dWdEWnJHWHZhc1g2eFpWbAozWGx6WGR3NHFMWjBFRk5td0FwaUNvU0dYUUtCZ1FETTl4bWtJT3V3V0lHYWorUm5kamZwVmU0cUZMN1FsMytwCk5ETjU3a3BjQTJOM2ZRaUFuMjBvYkFPdmlCQkhtZmVlNnMvNTdnd252WG5WSG9DeitsWEhOcWMzU1N1eFZZWGgKYkhjbW9QVWRTYnRCMGs5MFp6RS9oZW1zcEZWcVh0K3BBazIwZlZXN1Z0cE9YVS9jbWwzWFNpWC9pYzVuWUkxQwo1Q21WN21mbzN3S0JnUURGbnExYm81SllnVzVySGRvZGppcnBySi9QSVA5QjNTckZOMGFrYlpxQW1RSXdPem40CkJhRUpCSUkvWW44a21YRElRcmhKL1d3T3pSR1J5cml1Mm5zb1hHRk1iWFZqSHJYVnhnSFJRUml2SzN6Z2hld0gKYnRvVktPWkJwcnRqeHpud3Zsd002NnhCaURWNlNiYStialhUVGoxcG1BTjdDU3MyVVpKcCtsUzUzUUtCZ1FDagp5c04rMlovL0tIdEpuTHVWd204ajFEdWkvVGtQVUVJQVlXclJnWStDd0dKNFZqNjVLcTM4bzNTK2pTa1ZUd0VHCnlmYXhiMzNmTkJkc0xCZ0Z1NE5ZMDJBRldqL0VVdWZPSVBPR3R4R3ArZ1hlcjhZMjYwLzd4M1BaTnVuYk96M1kKdmJLRERiVzVJOHNJM0tDNVZkS3YvTjBvdkdOcFVVc1JyV3RlR1VxSVdRS0JnSDZEcDVIbG1uank1blV6azlIawp2VkFFZ3hyWVRjM0x0VkdCUHBZRHF0Vk5MYWxob3NYOENNcXZFeDdUeVhoRTJrdW8xV0NMRzhHMGNYbDV5SGMrClRIUzY2UVlpbEdSSTJIWUpzaXRxOHJ4S2tRUWV5MnZCekRBTHJCU2hGeFlKTHZTOURwUWhsREp5OUgzbklqK3EKeXloZXA4eWxaRUxtb1NGS25IcytRbEFRCi0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0K";

    private static X509Certificate2 CreateTestCertificate()
    {
        var certBytes = Convert.FromBase64String(CertPemBase64);
        var keyPem = Encoding.UTF8.GetString(Convert.FromBase64String(KeyPemBase64));

        using var cert = X509CertificateLoader.LoadCertificate(certBytes);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(keyPem);
        return cert.CopyWithPrivateKey(rsa);
    }

    [Fact]
    public async Task Import_LocalTlsEndpoint_ReturnsServedCertificate()
    {
        // Free port: bind, read the OS-assigned port, release it, reuse it for the server.
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        using var server = new TcpListener(IPAddress.Loopback, port);
        server.Start();
        var serverTask = Task.Run(async () =>
        {
            using var serverCert = CreateTestCertificate();
            try
            {
                // Handle at least one TLS GET with an empty 200.
                while (true)
                {
                    using var client = await server.AcceptTcpClientAsync();
                    client.NoDelay = true;
                    client.ReceiveTimeout = 10000;
                    using var ssl = new SslStream(client.GetStream(), false);
                    await ssl.AuthenticateAsServerAsync(serverCert, false, SslProtocols.None, false);
                    var buf = new byte[4096];
                    var read = await ssl.ReadAsync(buf, 0, buf.Length);
                    if (read <= 0)
                    {
                        continue;
                    }
                    var response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    await ssl.WriteAsync(response);
                    await ssl.FlushAsync();
                    // Client closes after 'Connection: close'; loop again for safety.
                }
            }
            catch (Exception)
            {
                // Listener closed or client disconnected; expected during test teardown.
            }
        });

        try
        {
            ICertificateLoader<string> importer = new RemoteServerImporter();
            var result = await importer.ImportAsync($"https://127.0.0.1:{port}");

            Assert.True(result.Success);
            Assert.NotNull(result.Certificates);
            Assert.NotEmpty(result.Certificates);
            // The served self-signed leaf must be among them (match by subject).
            Assert.Contains(result.Certificates, c => c.Subject == "CN=localhost");
        }
        finally
        {
            server.Stop();
            await serverTask;
        }
    }

    [Fact]
    public async Task Import_NoListener_ReturnsFailureWithException()
    {
        // A port with nothing listening: connection refused on loopback is immediate,
        // so this cannot hang (the importer's 10s timeout would bound it regardless).
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        ICertificateLoader<string> importer = new RemoteServerImporter();
        var result = await importer.ImportAsync($"https://127.0.0.1:{port}");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    [Trait("Category", "Network")]
    public async Task Check_RealNetwork()
    {
        string testAddress = "https://google.com";
        ICertificateLoader<string> importer = new RemoteServerImporter();
        ImportResult result = await importer.ImportAsync(testAddress);
        Assert.True(result.Success);
        Assert.NotNull(result.Certificates);
        Assert.NotEmpty(result.Certificates);
    }
}
