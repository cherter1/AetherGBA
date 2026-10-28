using System.IO;

namespace AetherGba.App.Wpf.Hosting;

internal static class RomDiscovery
{
    public static RomDiscoveryResult Resolve(string[] args, string appBaseDirectory)
    {
        var repoRoot = ResolveRepositoryRoot(appBaseDirectory);
        var romDirectory = Path.Combine(repoRoot, "roms");
        var biosDirectory = Path.Combine(repoRoot, "bios");
        var saveDirectory = Path.Combine(repoRoot, "saves");

        Directory.CreateDirectory(romDirectory);
        Directory.CreateDirectory(biosDirectory);
        Directory.CreateDirectory(saveDirectory);

        var biosPath = FindBios(biosDirectory, romDirectory);
        if (args.Length > 0)
        {
            var romPath = Path.GetFullPath(args[0], Directory.GetCurrentDirectory());
            return new RomDiscoveryResult(romPath, biosPath, saveDirectory, $"Loaded ROM from CLI path: {romPath}");
        }

        var roms = Directory.GetFiles(romDirectory, "*.gba", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return roms.Length switch
        {
            0 => new RomDiscoveryResult(null, biosPath, saveDirectory, $"No ROM found. Drop a .gba file into {romDirectory} or pass a ROM path on the command line."),
            1 => new RomDiscoveryResult(roms[0], biosPath, saveDirectory, $"Auto-loaded ROM: {Path.GetFileName(roms[0])}"),
            _ => new RomDiscoveryResult(roms[0], biosPath, saveDirectory, $"Multiple ROMs found. Defaulting to {Path.GetFileName(roms[0])}. Pass a ROM path to choose a different file."),
        };
    }

    private static string ResolveRepositoryRoot(string appBaseDirectory)
    {
        var candidate = Path.GetFullPath(Path.Combine(appBaseDirectory, "..", "..", "..", ".."));
        return Directory.Exists(candidate) ? candidate : Directory.GetCurrentDirectory();
    }

    private static string? FindBios(string biosDirectory, string romDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(biosDirectory, "gba_bios.bin"),
            Path.Combine(romDirectory, "bios.bin"),
            Path.Combine(romDirectory, "gba_bios.bin"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}

internal sealed record RomDiscoveryResult(
    string? RomPath,
    string? BiosPath,
    string SaveDirectory,
    string Message);