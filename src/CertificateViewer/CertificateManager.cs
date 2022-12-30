using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CertificateViewer.Logic;

namespace CertificateViewer;

public class CertificateManager
{
    private bool? isValid;
    private readonly ChainValidator _chainValidator = new();

    public CertificateManager()
    {
        Certificates.CollectionChanged += (_, _) => UpdateValidity();
        RootCertificates.CollectionChanged += (_, _) => UpdateValidity();

    }

    public ObservableCollection<X509Certificate2> Certificates { get; } = new();
    public ObservableCollection<X509Certificate2> RootCertificates { get; } = new();

    public bool? IsValid
    {
        get
        {
            UpdateValidity();
            return isValid;
        }
        private set => isValid = value;
    }

    public ObservableCollection<string> Errors { get; private set; } = new();

    public bool UseSystemStore { get; set; } = true;

    private void UpdateValidity()
    {
        if (Certificates.Any() == false)
        {
            IsValid = null;
            Errors = new ObservableCollection<string>();
            return;
        }
        IsValid = _chainValidator.Validate(Certificates, RootCertificates, UseSystemStore ,out var errors);
        Errors = new ObservableCollection<string>(errors);

    }

    public void LoadCertificates(List<X509Certificate2> certificates)
    {
        if (Certificates.Any())
        {
            Certificates.Clear();
        }
        certificates.ForEach(Certificates.Add);
        UpdateValidity();
    }
}
