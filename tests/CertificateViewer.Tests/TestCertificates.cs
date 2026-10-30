using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertificateViewer.Tests;

/// <summary>
///     Shared helpers for building real certificates in tests: a self-signed leaf and a
///     two-cert pair (a self-signed CA:TRUE root + a self-signed leaf), returned as raw
///     PEM/DER bytes and as <see cref="X509Certificate2" /> instances.
/// </summary>
internal static class TestCertificates
{
    /// <summary>Creates a self-signed leaf certificate.</summary>
    public static X509Certificate2 SelfSigned(string subject = "CN=Test")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    private static readonly RSA CaKey = RSA.Create(2048);
    private static readonly X509Certificate2 Ca = CreateCa();

    private static X509Certificate2 CreateCa()
    {
        var caRequest = new CertificateRequest("CN=Test CA", CaKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, false));
        return caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
    }

    /// <summary>
    ///     Creates a (leaf, ca) pair where the leaf is genuinely signed by the CA's private key.
    ///     When the leaf is offered together with the CA as a custom root, the chain validates
    ///     without touching the system store.
    /// </summary>
    public static (X509Certificate2 Leaf, X509Certificate2 Ca) Chain()
    {
        var (leaf, _) = SignWithCa("CN=Test Leaf");
        return (leaf, Ca);
    }

    /// <summary>Signs a new key pair with the shared CA and returns (leaf cert, leaf private key).</summary>
    private static (X509Certificate2, RSA) SignWithCa(string subject)
    {
        using var leafRsa = RSA.Create(2048);
        var leafRequest = new CertificateRequest(subject, leafRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        leafRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        var leaf = leafRequest.Create(Ca, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1), CaKey.ExportSubjectPublicKeyInfo());
        return (leaf, leafRsa);
    }

    /// <summary>Serializes a certificate to PEM bytes.</summary>
    public static byte[] ToPem(X509Certificate2 certificate)
    {
        var lines = new List<string> { "-----BEGIN CERTIFICATE-----" };
        var base64 = Convert.ToBase64String(certificate.RawData);
        for (var i = 0; i < base64.Length; i += 64)
        {
            lines.Add(base64.Substring(i, Math.Min(64, base64.Length - i)));
        }

        lines.Add("-----END CERTIFICATE-----");
        return Encoding.ASCII.GetBytes(string.Join("\n", lines));
    }

    /// <summary>Serializes a certificate to DER bytes.</summary>
    public static byte[] ToDer(X509Certificate2 certificate) => certificate.RawData.ToArray();
}
