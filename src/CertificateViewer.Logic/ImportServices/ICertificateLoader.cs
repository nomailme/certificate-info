namespace CertificateViewer.Logic.ImportServices;

public interface ICertificateLoader<in TIn, in TOptions>
    where TOptions : ICertificateLoaderOptions
{
    public Task<ImportResult> ImportAsync(TIn input, TOptions? options = default);
}

public interface ICertificateLoader<in TIn>: ICertificateLoader<TIn, EmptyOptions>
{
    public new Task<ImportResult> ImportAsync(TIn input, EmptyOptions? options = default);
}

public record EmptyOptions: ICertificateLoaderOptions
{
    public readonly static EmptyOptions Instance = new();
}