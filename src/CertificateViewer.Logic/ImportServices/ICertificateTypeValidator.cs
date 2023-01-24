namespace CertificateViewer.Logic.ImportServices;

public interface ICertificateTypeValidator<in TIn>
{
    bool IsSupported(TIn input);
}