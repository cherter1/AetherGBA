namespace AetherGba.Core.Memory;

public enum MemoryRegion : byte
{
    Unused,
    Bios,
    Ewram,
    Iwram,
    Io,
    PaletteRam,
    Vram,
    Oam,
    Rom,
    RomMirror,
    RomMirrorTwo,
    Sram
}