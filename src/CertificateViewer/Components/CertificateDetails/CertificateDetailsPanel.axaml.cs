using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;
using DynamicData;

namespace CertificateViewer.Components.CertificateDetails;

public partial class CertificateDetailsPanel : UserControl
{
    public static readonly DirectProperty<CertificateDetailsPanel, X509Certificate2?> CertificateProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, X509Certificate2?>(
            nameof(Certificate),
            o => o.Certificate,
            (o, v) => o.Certificate = v);

    public static readonly DirectProperty<CertificateDetailsPanel, bool?> ShowRawProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, bool?>(
            nameof(ShowRaw),
            o => o.ShowRaw,
            (o, v) => o.ShowRaw = v);


    public static readonly DirectProperty<CertificateDetailsPanel, string> RawDataProperty =
        AvaloniaProperty.RegisterDirect<CertificateDetailsPanel, string>(
            nameof(ShowRaw),
            o => o.RawData);

    private X509Certificate2? _certificate;
    private bool? _showRaw = false;

    static CertificateDetailsPanel() => AffectsRender<CertificateDetailsPanel>(CertificateProperty);

    public CertificateDetailsPanel() => InitializeComponent();

    public bool? ShowRaw
    {
        get => _showRaw;
        set => SetAndRaise(ShowRawProperty, ref _showRaw, value);
    }

    public X509Certificate2? Certificate
    {
        get => _certificate;
        set
        {
            SubjectAlternativeNames.Clear();
            SubjectAlternativeNames.Add(GetAlternativeDnsNames(value));

            SetAndRaise(CertificateProperty, ref _certificate, value);
            RaisePropertyChanged(RawDataProperty, string.Empty, Certificate?.ToString(true) ?? string.Empty);
        }
    }

    public string RawData => Certificate?.ToString(true) ?? string.Empty;

    public ObservableCollection<string> SubjectAlternativeNames { get; } = new();

    private static List<string> GetAlternativeDnsNames(X509Certificate2? cert)
    {
        const string SAN_OID = "2.5.29.17";

        if (cert is null)
        {
            return new List<string>();
        }

        var extension = cert.Extensions[SAN_OID];
        if (extension is null)
        {
            return new List<string>();
        }

        var dnsNameTag = new Asn1Tag(TagClass.ContextSpecific, 2);

        var asnReader = new AsnReader(extension.RawData, AsnEncodingRules.BER);
        var sequenceReader = asnReader.ReadSequence(Asn1Tag.Sequence);

        var resultList = new List<string>();

        while (sequenceReader.HasData)
        {
            var tag = sequenceReader.PeekTag();
            if (tag != dnsNameTag)
            {
                sequenceReader.ReadEncodedValue();
                continue;
            }

            var dnsName = sequenceReader.ReadCharacterString(UniversalTagNumber.IA5String, dnsNameTag);
            resultList.Add(dnsName);
        }

        return resultList;
    }
}
