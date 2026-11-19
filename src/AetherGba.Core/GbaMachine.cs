using System.Diagnostics;
using AetherGba.Core.Bios;
using AetherGba.Core.Common;
using AetherGba.Core.Cpu;
using AetherGba.Core.Dma;
using AetherGba.Core.Input;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;
using AetherGba.Core.Timers;
using AetherGba.Core.Video;
using GbaCartridge = AetherGba.Core.Cartridge.Cartridge;

namespace AetherGba.Core;

public sealed class GbaMachine
{
    private Arm7Tdmi Cpu { get; }
    public GbaBus Bus { get; }
    private Ppu Ppu { get; }
    public TimerController Timers { get; }
    private DmaController Dma { get; }
    public InterruptController Interrupts { get; }
    public KeypadState Keypad { get; }
    public GbaCartridge? Cartridge { get; }
    public FrameBuffer FrameBuffer => Ppu.FrameBuffer;
    private GbaEventHandler EventHandler { get; }

    private GbaMachine(
        Arm7Tdmi cpu,
        GbaBus bus,
        Ppu ppu,
        TimerController timers,
        DmaController dma,
        InterruptController interrupts,
        KeypadState keypad,
        GbaCartridge? cartridge, GbaEventHandler eventHandler)
    {
        Cpu = cpu;
        Bus = bus;
        Ppu = ppu;
        Timers = timers;
        Dma = dma;
        Interrupts = interrupts;
        Keypad = keypad;
        Cartridge = cartridge;
        EventHandler = eventHandler;
        EventHandler.AddScheduledEvent(new GbaEvent
        {
            Type = GbaEventType.EnterHBlank,
            CycleDue = 1006u, //hblank start cycle
            EventAction = ppu.EnterHBlank
        });
    }

    public static GbaMachine Create(GbaMachineOptions options)
    {
        var eventHandler = new GbaEventHandler();
        var memory = new GbaMemory(options.SaveDirectory ?? string.Empty, eventHandler);
        var interrupts = new InterruptController(memory);
        var keypad = new KeypadState(memory, interrupts);
        var timers = new TimerController(interrupts, eventHandler);
        memory.Io.ConnectTimerController(timers);
        memory.Io.ConnectInterruptController(interrupts);
        var dma = new DmaController(interrupts, memory);
        memory.Io.ConnectDmaController(dma);
        var ppu = new Ppu(interrupts, dma, memory, eventHandler);
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
        var machine = new GbaMachine(cpu, bus, ppu, timers, dma, interrupts, keypad, cartridge, eventHandler);
        machine.Reset();
        return machine;
    }

    private void Reset() => Cpu.Reset();

    public void RunFrame()
    {
        Ppu.FrameReady = false;
        while (!Ppu.FrameReady)
        {
            var instructionCycles = Cpu.Step();
            EventHandler.CurrentCycle += (uint)instructionCycles;

            if (EventHandler.CurrentCycle >= EventHandler.NextEventCycle)
            {
                RunScheduledEvents();
            }
        }
    }

    private void RunScheduledEvents()
    {
        while (EventHandler.CurrentCycle >= EventHandler.NextEventCycle)
        {
            var eventDue = EventHandler.Pop();
            eventDue.EventAction(Bus);
        }
    }
}