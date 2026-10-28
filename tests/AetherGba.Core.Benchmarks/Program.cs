using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using AetherGba.Core.Benchmarks.Cpu;
using AetherGba.Core.Cpu;

namespace AetherGba.Core.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ArmHalfwordLoadBenchmark>();

        //var armb = new ArmAluBenchmarks();
        //armb.Setup();
        //var unique = CpuOpt.ArmInstructionFetch.Distinct();
        //foreach (var instruction in unique)
        //{
            //Console.WriteLine(instruction);
        //}
        //armb.Opt_StepArm_AndWithRotate();
        //armb.StepArm_AndWithRotate();
        //Console.WriteLine(armb._cpu.Registers[3]);
        //Console.WriteLine(armb._cpuOpt.Registers[3]);

        //var armb = new ArmSingleDataTransferBenchmarks();
        //armb.Setup();
        //armb.Opt_StepArm_Ldr();
        //armb.StepArm_Ldr();
        //armb.Opt_StepArm_Str();
        //armb.StepArm_Str();
        //Console.WriteLine(armb._loadCpu.Registers[0]);
        //Console.WriteLine(armb._loadCpuOpt.Registers[0]);
        //Console.WriteLine(armb._storeBus.Read32(0x03000004));
        //Console.WriteLine(armb._storeBusOpt.Read32(0x03000004));

        //var armb = new ArmHalfwordLoadBenchmark();
        //armb.Setup();
        //armb.Opt_StepArm_Ldrh();
        //armb.StepArm_Ldrh();
        //Console.WriteLine(armb._loadCpu.Registers[1]);
        //Console.WriteLine(armb._loadCpuOpt.Registers[1]);

        //var armb = new ArmHalfwordStoreBenchmark();
        //armb.Setup();
        //armb.Opt_StepArm_Strh();
        //armb.StepArm_Strh();
        //Console.WriteLine(armb._storeBusOpt.Read16(0x03000004));
        //Console.WriteLine(armb._storeBus.Read16(0x03000004));
    }
}