namespace CertificateViewer.Logic.ImportServices;

public interface IImporter<in TIn>
{
    Task<ImportResult> ImportAsync(TIn input, params object[] parameters);
}