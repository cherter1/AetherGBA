using AetherGba.Core.Cpu;

namespace AetherGba.Core.Common;

public sealed record CpuTrace(
    uint InstructionAddress,
    uint RawInstruction,
    bool ThumbState,
    CpuMode Mode,
    uint R0,
    uint R1,
    uint R2,
    uint R3,
    uint R12,
    uint Sp,
    uint Lr,
    uint PcBefore,
    uint PcAfter,
    uint Cpsr,
    string Decoded);