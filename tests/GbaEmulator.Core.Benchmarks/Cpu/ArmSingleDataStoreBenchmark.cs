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
public class ArmSingleDataStoreBenchmark
{
    private const int StepsPerInvoke = 16_384;
    private const uint RomBase = 0x08000000;

    public Arm7Tdmi _storeCpu = null!;
    public GbaBus _storeBus = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_storeCpu, _storeBus) = CpuUtilities.CreateCpu();
        _storeCpu.Reset();

        byte[] rom = new byte[StepsPerInvoke * sizeof(uint)];

        for (int offset = 0; offset < rom.Length; offset += sizeof(uint))
        {
            rom[offset + 0] = 0xa2;
            rom[offset + 1] = 0x00;
            rom[offset + 2] = 0x81;
            rom[offset + 3] = 0xe7;
        }

        _storeBus.LoadCartridge(new Cartridge.Cartridge("store.gba", rom));

        _storeCpu.Registers[0] = 0x11abcdef;
        _storeCpu.Registers[1] = 0x03000000;
        _storeCpu.Registers[2] = 0x8;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = StepsPerInvoke)]
    public int StepArm_Str()
    {
        _storeCpu.Registers.ProgramCounter = RomBase;
        int totalCycles = 0;

        for (int i = 0; i < StepsPerInvoke; i++)
        {
            totalCycles = _storeCpu.Step();
        }

        return totalCycles;
    }
}