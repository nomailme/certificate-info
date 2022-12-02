using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MugenMvvmToolkit;

namespace CertificateViewerPlayground;

public class  CertificateFileInfo
{
    private readonly RootCertificateStore rootCertificateStore;

    public CertificateFileInfo(X509Certificate2Collection certificates, RootCertificateStore rootCertificateStore)
    {
        this.rootCertificateStore = rootCertificateStore;
        Certificates = certificates;
    }

    public X509Certificate2Collection Certificates { get; set; }

    public List<string> Validate()
    {
        var chain = X509Chain.Create();
        var policy = new X509ChainPolicy
        {
            RevocationMode = X509RevocationMode.NoCheck,
            RevocationFlag = X509RevocationFlag.EntireChain,
            TrustMode = rootCertificateStore.UseSystemRootStore? X509ChainTrustMode.System : X509ChainTrustMode.CustomRootTrust
        };
        if (rootCertificateStore.UseSystemRootStore == false)
        {
            policy.CustomTrustStore.AddRange(rootCertificateStore.RootCertificates);
        }
        Certificates.Skip(1).ForEach(x=>policy.ExtraStore.Add(x));
        chain.ChainPolicy = policy;

        var result = chain.Build(Certificates.First());
        return result ? new List<string>() : chain.ChainStatus.Select(x => x.StatusInformation).ToList();
    }
}
