using System.Numerics;
using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
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
    private void Ldm(ushort instruction)
    {
        var transferCount = BitOperations.PopCount(instruction & 0xFFu);
        var rb = (instruction >> 8) & 0b111;
        var address = Registers[rb];
        var finalAddress = address + (uint)(transferCount * 4);
        if (transferCount == 0)
        {
            Registers[rb] += 0x40;
            Registers[15] = _bus.Read32(address & ~3u);

            _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false);
            return;
        }

        for (int reg = 0; reg < 8; reg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            Registers[reg] = _bus.Read32(address);
            _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: reg != BitOperations.TrailingZeroCount(instruction)); //first N then S

            address += 4u;
        }

        if (!BitUtils.IsBitSet(instruction, rb))
        {
            Registers[rb] = finalAddress;
        }
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    private void Stm(ushort instruction)
    {
        var transferCount = BitOperations.PopCount(instruction & 0xFFu);
        var rb = (instruction >> 8) & 0b111;
        var address = Registers[rb];
        var finalAddress = address + (uint)(transferCount * 4);
        if (transferCount == 0)
        {
            Registers[rb] += 0x40;
            _bus.Write32(address, Registers[15] + 4);
            _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false);
            return;
        }

        for (int reg = 0; reg < 8; reg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            if (reg == rb && reg != BitOperations.TrailingZeroCount(instruction))
            {
                _bus.Write32(address, finalAddress);
            }
            else
            {
                _bus.Write32(address, Registers[reg]);
            }
            _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: reg != BitOperations.TrailingZeroCount(instruction));

            address += 4u;
        }

        Registers[rb] = finalAddress;
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }
    #endregion

    #region Format14
    private void Push(ushort instruction)
    {
        for (int reg = 8; reg >= 0; reg--)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            Registers[13] -= 4;
            var register = Registers[reg];
            _bus.Write32(Registers.StackPointer, reg == 8 ? Registers.LinkRegister : register);
            _cycles += _bus.GetCpuAccessCycles(Registers.StackPointer, AccessWidth.Halfword, sequential: BitOperations.TrailingZeroCount(instruction) != reg);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void Pop(ushort instruction)
    {
        for (int reg = 0; reg <= 8; reg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, reg);
            if (!shouldTransfer)
                continue;

            var result = _bus.Read32(Registers.StackPointer & ~3u);

            _cycles += _bus.GetCpuAccessCycles(Registers.StackPointer, AccessWidth.Halfword,
                sequential: BitOperations.TrailingZeroCount(instruction) != reg);

            if (reg == 8)
            {
                Registers[15] = result & ~1u;

                //refill pipeline
                _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
                _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
            }
            else
            {
                Registers[reg] = result;
            }
            Registers[13] += 4;
        }

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format8
    private void Strh(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        _bus.Write16(effectiveAddress, (ushort)Registers[rd]);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false);
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void Ldsb(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        var loadedByte = _bus.Read8(effectiveAddress);
        Registers[rd] = (uint)BitUtils.SignExtend(loadedByte, 8);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    private void Ldrh(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        uint value = _bus.Read16(effectiveAddress);
        if ((effectiveAddress & 1) != 0)
        {
            value = BitOperations.RotateRight(value, 8);
        }

        Registers[rd] = value;

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    private void Ldsh(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        var loadedHalfword = _bus.Read16(effectiveAddress);
        if ((effectiveAddress & 1) != 0)
        {
            Registers[rd] = (uint)BitUtils.SignExtend((loadedHalfword >> 8) & 0xff, 8);
        }
        else
        {
            Registers[rd] = (uint)BitUtils.SignExtend(loadedHalfword, 16);
        }

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format10

    private void StrhImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 1;
        var effectiveAddress = Registers[rb] + (uint)offset;

        _bus.Write16(effectiveAddress, (ushort)Registers[rd]);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void LdrhImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 1;
        var effectiveAddress = Registers[rb] + (uint)offset;

        uint value = _bus.Read16(effectiveAddress);
        if ((effectiveAddress & 1) != 0)
        {
            value = BitOperations.RotateRight(value, 8);
        }

        Registers[rd] = value;

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion
}