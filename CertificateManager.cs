using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MugenMvvmToolkit;

namespace CertificateViewer;

public class CertificateManager : IDisposable
{
    private bool? isValid;
    public CertificateManager()
    {
        Certificates.CollectionChanged += (_, _) => OnOnCollectionChanged(EventArgs.Empty);
        RootCertificates.CollectionChanged += (_, _) => OnOnCollectionChanged(EventArgs.Empty);

    }

    public ObservableCollection<CertificateVm> Certificates { get; } = new();
    public ObservableCollection<CertificateVm> RootCertificates { get; } = new();

    public bool? IsValid
    {
        get
        {
            UpdateValidity();
            return isValid;
        }
        private set => isValid = value;
    }

    public ObservableCollection<string> Errors { get; set; } = new();

    public bool UseSystemStore { get; set; } = true;

    public void Dispose()
    {
        OnCollectionChanged = null;
        GC.SuppressFinalize(this);
    }
    public event EventHandler? OnCollectionChanged;

    private void OnOnCollectionChanged(EventArgs eventArgs)
    {
        UpdateValidity();
        OnCollectionChanged?.Invoke(this, eventArgs);
    }

    public void UpdateValidity()
    {
        var errors = Validate();
        if (errors is not null)
        {
            IsValid = errors.IsEmpty();
            Errors = new ObservableCollection<string>(errors);
        }
    }

    private List<string>? Validate()
    {
        if (Certificates.IsEmpty())
        {
            return null;
        }
        var chain = X509Chain.Create();
        var policy = new X509ChainPolicy
        {
            RevocationMode = X509RevocationMode.NoCheck,
            RevocationFlag = X509RevocationFlag.EntireChain,
            TrustMode = UseSystemStore ? X509ChainTrustMode.System : X509ChainTrustMode.CustomRootTrust
        };
        if (UseSystemStore == false)
        {
            policy.CustomTrustStore.AddRange(RootCertificates.Select(x => x.Certificate).ToArray());
        }
        Certificates.Skip(1).ForEach(x => policy.ExtraStore.Add(x.Certificate));
        chain.ChainPolicy = policy;

        var result = chain.Build(Certificates.Select(x => x.Certificate).First());
        return result ? new List<string>() : chain.ChainStatus.Select(x => x.StatusInformation).ToList();
    }

    public void LoadCertificates(List<X509Certificate2> resultCertificates)
    {
        var viewModels = resultCertificates.Select(x => new CertificateVm(x));
        if (Certificates.Any())
        {
            Certificates.Clear();
        }
        Certificates.AddRange(viewModels);
        UpdateValidity();
    }
}
