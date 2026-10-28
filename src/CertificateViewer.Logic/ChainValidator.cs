using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer.Logic;

public class ChainValidator
{
    public async Task<List<string>> Validate(ICollection<X509Certificate2> certificates, ICollection<X509Certificate2>? rootCertificates, bool useSystemStore)
    {
        await Task.CompletedTask;
        ValidateCore(certificates, rootCertificates, useSystemStore, out var errors);
        return errors;
    }

    private bool ValidateCore(ICollection<X509Certificate2> certificates, ICollection<X509Certificate2>? rootCertificates, bool useSystemStore, out List<string> errors)
    {
        using var chain = X509Chain.Create();
        var policy = new X509ChainPolicy
        {
            RevocationMode = X509RevocationMode.NoCheck,
            RevocationFlag = X509RevocationFlag.EntireChain,
            TrustMode = useSystemStore ? X509ChainTrustMode.System : X509ChainTrustMode.CustomRootTrust
        };
        
        chain.ChainPolicy = policy;

        if (useSystemStore==false)
        {
            if (rootCertificates != null)
            {
                foreach (var root in rootCertificates)
                    policy.CustomTrustStore.Add(root);
            }
        }
        foreach (var x in certificates.Skip(1))
            policy.ExtraStore.Add(x);
        
        if (certificates.Count == 0)
        {
            errors = new List<string> { "No certificate to validate." };
            return false;
        }

        var result = chain.Build(certificates.First());
        errors = chain.ChainStatus.Select(x => x.StatusInformation).ToList();
        return result;
    }
}
