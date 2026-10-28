using AetherGba.Core;

namespace AetherGba.App.SDL.Hosting;

public static class GbaInit
{
    public static GbaMachine CreateMachine()
    {
        var rom = ResolveGbaRomPath();
        var bios = ResolveGbaBiosPath();
        var save = ResolveSaveFilePath(rom);

        var machine = GbaMachine.Create(new GbaMachineOptions { BiosPath = bios, RomPath = rom, SaveDirectory = save });
        return machine;
    }

    private static string? ResolveGbaRomPath()
    {
        var romsDirectory = AppContext.BaseDirectory + "roms";

        var topRom =
            Directory.GetFiles(romsDirectory, "*.gba", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        return topRom;
    }

    private static string? ResolveGbaBiosPath()
    {
        var biosFile = Path.Combine(AppContext.BaseDirectory, "bios", "gba_bios.bin");
        return File.Exists(biosFile) ? biosFile : null;
    }

    private static string? ResolveSaveFilePath(string? romPath)
    {
        if (romPath == null) return null;

        var saveFileName = Path.GetFileName(romPath).Replace(".gba", ".sav");
        var saveFilePath = Path.Combine(AppContext.BaseDirectory, saveFileName);
        return File.Exists(saveFilePath) ? saveFilePath : null;
    }
}