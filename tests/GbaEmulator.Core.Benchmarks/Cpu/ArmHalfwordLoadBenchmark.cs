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
[DisassemblyDiagnoser(maxDepth: 1, printSource: true, exportCombinedDisassemblyReport: true, exportDiff: true)]
public class ArmHalfwordLoadBenchmark
{
    private const int StepsPerInvoke = 16_384;
    private const uint RomBase = 0x08000000;

    public Arm7Tdmi _loadCpu = null!;
    public GbaBus _loadBus = null!;

    [GlobalSetup]
    public void Setup()
    {
        (_loadCpu, _loadBus) = CpuUtilities.CreateCpu();
        _loadCpu.Reset();

        byte[] rom = new byte[StepsPerInvoke * sizeof(uint)];

        for (int offset = 0; offset < rom.Length; offset += sizeof(uint))
        {
            rom[offset + 0] = 0xb3;
            rom[offset + 1] = 0x10;
            rom[offset + 2] = 0x92;
            rom[offset + 3] = 0xe1;
        }

        _loadBus.LoadCartridge(new Cartridge.Cartridge("load.gba", rom));

        _loadCpu.Registers[1] = 0x10;
        _loadCpu.Registers[2] = 0x03000000;
        _loadCpu.Registers[3] = 0x4;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = StepsPerInvoke)]
    public int StepArm_Ldrh()
    {
        _loadCpu.Registers.ProgramCounter = RomBase;
        int totalCycles = 0;

        for (int i = 0; i < StepsPerInvoke; i++)
        {
            totalCycles = _loadCpu.Step();
        }

        return totalCycles;
    }
}