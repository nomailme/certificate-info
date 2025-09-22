using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Data.Converters;

namespace CertificateViewer.Converters;

public class SubjectAlternativeNamesConverter:IValueConverter
{

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {

        if (value == null)
        {
            return new List<string>();
        }

        if (value is not X509Certificate2 cert)
        {
            throw new ArgumentException("Value is not a certificate");
        }

        return GetAlternativeDnsNames(cert);
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();


    private static List<string> GetAlternativeDnsNames(X509Certificate2 cert)
    {
        const string SAN_OID = "2.5.29.17";

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
