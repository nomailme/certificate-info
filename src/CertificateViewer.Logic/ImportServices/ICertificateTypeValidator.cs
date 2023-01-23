namespace CertificateViewer.Logic.ImportServices;

public interface ICertificateTypeValidator<TIn>
{
    bool IsSupported(TIn input);
}