namespace CertificateViewer.Logic.ImportServices;

public class MagicByteHelper
{
    protected async ValueTask<bool> ValidateUsingMagicBytes(Stream stream, IReadOnlyCollection<byte> magicBytes)
    {
        var buffer = new byte[magicBytes.Count];
        await stream.ReadExactlyAsync(buffer, 0, magicBytes.Count);
        return buffer.SequenceEqual(magicBytes);
    }
    
    public static bool Match(IReadOnlyCollection<byte> input, IReadOnlyCollection<byte> magicBytes)
    {
        return input.Take(magicBytes.Count).SequenceEqual(magicBytes);
    }
}