using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic;

public class ChainValidator
{
    public bool Validate(ICollection<X509Certificate2> certificates, ICollection<X509Certificate2>? rootCertificates, bool useSystemStore, out List<string> errors)
    {
        return this.ValidateCore(certificates, rootCertificates, useSystemStore, out errors);
    }

    private bool ValidateCore(ICollection<X509Certificate2> certificates, ICollection<X509Certificate2>? rootCertificates, bool useSystemStore, out List<string> errors)
    {
        var chain = X509Chain.Create();
        var policy = new X509ChainPolicy
        {
            RevocationMode = X509RevocationMode.NoCheck,
            RevocationFlag = X509RevocationFlag.EntireChain,
            TrustMode = useSystemStore ? X509ChainTrustMode.System : X509ChainTrustMode.CustomRootTrust
        };
        
        chain.ChainPolicy = policy;

        if (useSystemStore==false)
        {
            rootCertificates?.ForEach(root => policy.CustomTrustStore.Add(root));
        }
        certificates.Skip(1).ForEach(x=>policy.ExtraStore.Add(x));
        
        var result = chain.Build(certificates.First());
        errors = chain.ChainStatus.Select(x => x.StatusInformation).ToList();
        return result;
    }
}