namespace CertificateViewer.Logic.ImportServices;

public abstract class CertificateLoader<TIn, TOptions>
    where TOptions : ICertificateLoaderOptions
{
    public Task<ImportResult> ImportAsync(TIn input, TOptions options)
    {
        return this.ImportCore(input, options);
    }

    protected abstract Task<ImportResult> ImportCore(TIn input, TOptions options);
}

public abstract class CertificateLoader<TIn>: CertificateLoader<TIn, EmptyOptions>
{
    public Task<ImportResult> ImportAsync(TIn input)
    {
        return ImportAsync(input, new EmptyOptions());
    }

    protected override abstract Task<ImportResult> ImportCore(TIn input, EmptyOptions? options);
}

public class EmptyOptions: ICertificateLoaderOptions
{
}