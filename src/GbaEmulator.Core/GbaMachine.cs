using System.Diagnostics;
using GbaEmulator.Core.Bios;
using GbaEmulator.Core.Cpu;
using GbaEmulator.Core.Dma;
using GbaEmulator.Core.Input;
using GbaEmulator.Core.Interrupts;
using GbaEmulator.Core.Memory;
using GbaEmulator.Core.Timers;
using GbaEmulator.Core.Video;
using GbaCartridge = GbaEmulator.Core.Cartridge.Cartridge;

namespace GbaEmulator.Core;

public sealed class GbaMachine
{
    private Arm7Tdmi Cpu { get; }
    private GbaBus Bus { get; }
    private Ppu Ppu { get; }
    private TimerController Timers { get; }
    private DmaController Dma { get; }
    public InterruptController Interrupts { get; }
    public KeypadState Keypad { get; }
    public GbaCartridge? Cartridge { get; }
    public FrameBuffer FrameBuffer => Ppu.FrameBuffer;

    private GbaMachine(
        Arm7Tdmi cpu,
        GbaBus bus,
        Ppu ppu,
        TimerController timers,
        DmaController dma,
        InterruptController interrupts,
        KeypadState keypad,
        GbaCartridge? cartridge)
    {
        Cpu = cpu;
        Bus = bus;
        Ppu = ppu;
        Timers = timers;
        Dma = dma;
        Interrupts = interrupts;
        Keypad = keypad;
        Cartridge = cartridge;
    }

    public static GbaMachine Create(GbaMachineOptions options)
    {
        var memory = new GbaMemory();
        var interrupts = new InterruptController(memory);
        var keypad = new KeypadState(memory, interrupts);
        var timers = new TimerController(interrupts);
        memory.Io.ConnectTimerController(timers);
        memory.Io.ConnectInterruptController(interrupts);
        var dma = new DmaController(interrupts, memory);
        memory.Io.ConnectDmaController(dma);
        var ppu = new Ppu(interrupts, dma, memory);
        var bus = new GbaBus(memory);
        var cpu = new Arm7Tdmi(bus, interrupts);

        var cartridge = options.RomPath is { Length: > 0 } romPath && File.Exists(romPath)
            ? GbaCartridge.Load(romPath)
            : null;

        bus.LoadCartridge(cartridge);
        Span<string> saveStrings =
        [
            "NONSENSE_NOVALIDSAVEMATCHING_NONSENSE",
            "EEPROM_",
            "SRAM_",
            "FLASH_",
            "FLASH512_",
            "FLASH1M_"
        ];
        int matchedIndex = 0;
        if (cartridge == null) goto breakLoop;
        for (int i = 0; i < saveStrings.Length; i++)
        {
            char[] chars = saveStrings[i].ToCharArray();
            int matchLength = 0;
            for (int j = 0; j < cartridge.RomData.Length; j++)
            {
                if (cartridge.RomData[j] == chars[matchLength])
                {
                    matchLength++;
                    if (matchLength >= chars.Length)
                    {
                        matchedIndex = i;
                        Console.WriteLine($"{j:x8}");
                        goto breakLoop;
                    }
                }
                else
                {
                    matchLength = 0;
                }
            }
        }
        breakLoop:
        switch (matchedIndex)
        {
            case 0:
                Console.WriteLine("nothing matched");
                break;
            case 1:
                Console.WriteLine("EEPROM matched");
                break;
            case 2:
                Console.WriteLine("SRAM matched");
                break;
            case 3:
                Console.WriteLine("FLASH matched");
                break;
            case 4:
                Console.WriteLine("FLASH512 matched");
                break;
            case 5:
                Console.WriteLine("FLASH1M matched");
                break;
        }

        bus.LoadBios(BiosImage.LoadOptional(options.BiosPath));
        var machine = new GbaMachine(cpu, bus, ppu, timers, dma, interrupts, keypad, cartridge);
        machine.Reset();
        return machine;
    }

    private void Reset() => Cpu.Reset();

    public void RunFrame()
    {
        Ppu.FrameReady = false;
        //var iterations = 0;
        //var consumed = 0;
        //var cpuWatch = new Stopwatch();
        //var dmaWatch = new Stopwatch();
        //var timerWatch = new Stopwatch();
        //var ppuWatch = new Stopwatch();
        //while (consumed < cycles)
        while (!Ppu.FrameReady)
        {
            //cpuWatch.Start();
            var instructionCycles = Cpu.Step();
            //cpuWatch.Stop();

            //dmaWatch.Start();
            Dma.RunDmas(DmaTimingType.Immediately, Bus);
            //dmaWatch.Stop();

            //timerWatch.Start();
            Timers.Advance(instructionCycles);
            //timerWatch.Stop();

            //ppuWatch.Start();
            Ppu.Step(instructionCycles, Bus);
            //ppuWatch.Stop();

            //consumed += instructionCycles;
            //iterations += 1;
        }
        //Console.WriteLine($"{iterations} iterations completed");
        //Console.WriteLine($"{cpuWatch.Elapsed.TotalMilliseconds} ms in CPU");
        //Console.WriteLine($"{dmaWatch.Elapsed.TotalMilliseconds} ms in DMA");
        //Console.WriteLine($"{timerWatch.Elapsed.TotalMilliseconds} ms in timers");
        //Console.WriteLine($"{ppuWatch.Elapsed.TotalMilliseconds} ms in ppu");
    }
}