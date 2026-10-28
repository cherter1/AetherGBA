using AetherGba.Core;

namespace AetherGba.App.Wpf.Hosting;

public sealed class EmulatorStartup
{
    public required GbaMachine Machine { get; init; }
    public required string StatusMessage { get; init; }
    public required string WindowTitle { get; init; }

    public static EmulatorStartup Create(string[] args, string appBaseDirectory)
    {
        var startup = RomDiscovery.Resolve(args, appBaseDirectory);
        var machine = GbaMachine.Create(new GbaMachineOptions
        {
            RomPath = startup.RomPath,
            BiosPath = startup.BiosPath,
            SaveDirectory = startup.SaveDirectory,
        });

        var title = machine.Cartridge?.Title is { Length: > 0 } romTitle
            ? $"GBA Emulator - {romTitle}"
            : "GBA Emulator";

        return new EmulatorStartup
        {
            Machine = machine,
            StatusMessage = startup.Message,
            WindowTitle = title
        };
    }
}