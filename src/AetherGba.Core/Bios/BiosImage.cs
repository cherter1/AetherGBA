namespace AetherGba.Core.Bios;

public sealed class BiosImage
{
    private BiosImage(byte[] bytes)
    {
        Bytes = bytes;
    }

    public byte[] Bytes { get; }

    public static BiosImage LoadOptional(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new BiosImage(HleBios.Bios);
        }

        return new BiosImage(File.ReadAllBytes(path));
    }
}