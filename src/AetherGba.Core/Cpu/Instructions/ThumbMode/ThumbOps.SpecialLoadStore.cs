using System.Numerics;
using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ThumbMode;

public static partial class ThumbOps
{
    /* BRANCH AND SWI INSTRUCTION ENCODINGS

      |..........1 ..................0|
      |5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |1_1_0_0|L|_Rb__|____RList______| multiple load/store
      |1_0_1_1|L|1_0|R|____RList______| push/pop registers
      |0_1_0_1|H|S|1|_Ro__|_Rb__|_Rd__| Load/Store sign-extended byte/halfword
      |1_0_0_0|L|_Offset5_|_Rb__|_Rd__| Load/Store halfword
     */

    #region Format15
    public static void Ldm(Arm7Tdmi cpu, ushort instruction)
    {
        var transferCount = BitOperations.PopCount(instruction & 0xFFu);
        var rb = (instruction >> 8) & 0b111;
        var address = cpu.Registers[rb];
        var finalAddress = address + (uint)(transferCount * 4);
        if (transferCount == 0)
        {
            cpu.Registers[rb] += 0x40;
            cpu.Registers[15] = cpu.Bus.Read32(address & ~3u);

            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false);
            return;
        }

        for (int reg = 0; reg < 8; reg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            cpu.Registers[reg] = cpu.Bus.Read32(address);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: reg != BitOperations.TrailingZeroCount(instruction)); //first N then S

            address += 4u;
        }

        if (!BitUtils.IsBitSet(instruction, rb))
        {
            cpu.Registers[rb] = finalAddress;
        }
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    public static void Stm(Arm7Tdmi cpu, ushort instruction)
    {
        var transferCount = BitOperations.PopCount(instruction & 0xFFu);
        var rb = (instruction >> 8) & 0b111;
        var address = cpu.Registers[rb];
        var finalAddress = address + (uint)(transferCount * 4);
        if (transferCount == 0)
        {
            cpu.Registers[rb] += 0x40;
            cpu.Bus.Write32(address, cpu.Registers[15] + 4);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false);
            return;
        }

        for (int reg = 0; reg < 8; reg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            if (reg == rb && reg != BitOperations.TrailingZeroCount(instruction))
            {
                cpu.Bus.Write32(address, finalAddress);
            }
            else
            {
                cpu.Bus.Write32(address, cpu.Registers[reg]);
            }
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: reg != BitOperations.TrailingZeroCount(instruction));

            address += 4u;
        }

        cpu.Registers[rb] = finalAddress;
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }
    #endregion

    #region Format14
    public static void Push(Arm7Tdmi cpu, ushort instruction)
    {
        for (int reg = 8; reg >= 0; reg--)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            cpu.Registers[13] -= 4;
            var register = cpu.Registers[reg];
            cpu.Bus.Write32(cpu.Registers.StackPointer, reg == 8 ? cpu.Registers.LinkRegister : register);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.StackPointer, AccessWidth.Halfword, sequential: BitOperations.TrailingZeroCount(instruction) != reg);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void Pop(Arm7Tdmi cpu, ushort instruction)
    {
        for (int reg = 0; reg <= 8; reg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            var result = cpu.Bus.Read32(cpu.Registers.StackPointer & ~3u);

            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.StackPointer, AccessWidth.Halfword,
                sequential: BitOperations.TrailingZeroCount(instruction) != reg);

            if (reg == 8)
            {
                cpu.Registers[15] = result & ~1u;

                //refill pipeline
                cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
                cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
            }
            else
            {
                cpu.Registers[reg] = result;
            }
            cpu.Registers[13] += 4;
        }

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format8
    public static void Strh(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        cpu.Bus.Write16(effectiveAddress, (ushort)cpu.Registers[rd]);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false);
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void Ldsb(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        var loadedByte = cpu.Bus.Read8(effectiveAddress);
        cpu.Registers[rd] = (uint)BitUtils.SignExtend(loadedByte, 8);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    public static void Ldrh(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        uint value = cpu.Bus.Read16(effectiveAddress);
        if ((effectiveAddress & 1) != 0)
        {
            value = BitOperations.RotateRight(value, 8);
        }

        cpu.Registers[rd] = value;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    public static void Ldsh(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        var loadedHalfword = cpu.Bus.Read16(effectiveAddress);
        if ((effectiveAddress & 1) != 0)
        {
            cpu.Registers[rd] = (uint)BitUtils.SignExtend((loadedHalfword >> 8) & 0xff, 8);
        }
        else
        {
            cpu.Registers[rd] = (uint)BitUtils.SignExtend(loadedHalfword, 16);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format10

    public static void StrhImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 1;
        var effectiveAddress = cpu.Registers[rb] + (uint)offset;

        cpu.Bus.Write16(effectiveAddress, (ushort)cpu.Registers[rd]);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void LdrhImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 1;
        var effectiveAddress = cpu.Registers[rb] + (uint)offset;

        uint value = cpu.Bus.Read16(effectiveAddress);
        if ((effectiveAddress & 1) != 0)
        {
            value = BitOperations.RotateRight(value, 8);
        }

        cpu.Registers[rd] = value;

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion
}