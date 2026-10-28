namespace CertificateViewer.Logic.ImportServices;

public class MagicByteHelper
{
    public static bool Match(IReadOnlyCollection<byte> input, IReadOnlyCollection<byte> magicBytes)
    {
        return input.Take(magicBytes.Count).SequenceEqual(magicBytes);
    }
}