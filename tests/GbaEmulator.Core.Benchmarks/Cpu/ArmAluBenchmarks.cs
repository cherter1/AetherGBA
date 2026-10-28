using BenchmarkDotNet.Attributes;
using GbaEmulator.Core.Cpu;
using GbaEmulator.Core.Memory;
using GbaEmulator.Core.Tests.TestUtils;

namespace GbaEmulator.Core.Benchmarks.Cpu;

[MemoryDiagnoser]
[SimpleJob(launchCount: 3)]
[MinIterationTime(250)]
[MaxRelativeError(0.005)]
[StatisticalTestColumn("1%")]
public class ArmAluBenchmarks
{
    private const int StepsPerInvoke = 16_384;
    private const uint RomBase = 0x08000000;

    public Arm7Tdmi _cpu = null!;
    public GbaBus _bus = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_cpu, _bus) = CpuUtilities.CreateCpu();
        _cpu.Reset();

        byte[] rom = new byte[StepsPerInvoke * sizeof(uint)];

        for (int offset = 0; offset < rom.Length; offset += sizeof(uint))
        {
            rom[offset + 0] = 0x31;
            rom[offset + 1] = 0x32;
            rom[offset + 2] = 0x10;
            rom[offset + 3] = 0xe0;
        }

        _bus.LoadCartridge(new Cartridge.Cartridge("and.gba", rom));

        _cpu.Registers[0] = 0b11;
        _cpu.Registers[1] = 0b10;
        _cpu.Registers[2] = 1;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = StepsPerInvoke)]
    public int StepArm_AndWithRotate()
    {
        _cpu.Registers.ProgramCounter = RomBase;
        int totalCycles = 0;

        for (int i = 0; i < StepsPerInvoke; i++)
        {
            totalCycles = _cpu.Step();
        }

        return totalCycles;
    }

}