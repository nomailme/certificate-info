using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace CertificateViewer;

public sealed class CertificateVm : INotifyPropertyChanged
{
    private X509Certificate2? certificate;

    public CertificateVm(X509Certificate2 certificate) => Certificate = certificate;

    public string Subject => $"Subject {Certificate?.Subject}";
    public string Issuer => $"Issuer {Certificate?.Issuer}";

    public string Validity => $"{Certificate?.NotBefore:d}-{Certificate?.NotAfter:d}";

    public X509Certificate2? Certificate
    {
        get => certificate;
        set
        {
            if (SetField(ref certificate, value))
            {
                OnPropertyChanged(nameof(Subject));
                OnPropertyChanged(nameof(Issuer));
                OnPropertyChanged(nameof(Validity));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;


    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

