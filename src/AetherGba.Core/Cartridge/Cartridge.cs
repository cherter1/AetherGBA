namespace AetherGba.Core.Cartridge;

public sealed class Cartridge
{
    public string Path { get; }
    public byte[] RomData { get; }
    public string Title { get; }

    public Cartridge(string path, byte[] romData)
    {
        Path = path;
        RomData = romData;
        Title = ExtractTitle(romData);
    }

    public static Cartridge Load(string path) => new(path, File.ReadAllBytes(path));

    private static string ExtractTitle(byte[] romData)
    {
        return romData.Length < 0xAC
            ? string.Empty
            : System.Text.Encoding.ASCII.GetString(romData, 0xA0, 12).TrimEnd('\0', ' ');
    }
}