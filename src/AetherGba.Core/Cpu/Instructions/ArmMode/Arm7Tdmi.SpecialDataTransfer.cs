using System.Numerics;
using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* SPECIAL DATA TRANSFER INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|1_0_0|P|U|S|W|L|__Rn___|_________Register_List_________| LDM, STM
      |_Cond__|0_0_0_1_0|B|0_0|__Rn___|__Rd___|0_0_0_0|1_0_0_1|__Rm___| SWP, SWPB
      |_Cond__|0_0_0|P|U|0|W|L|__Rn___|__Rd___|0_0_0_0|1|S|H|1|__Rm___| reg offset
      |_Cond__|0_0_0|P|U|1|W|L|__Rn___|__Rd___|_H_Off_|1|S|H|1|_L_Off_| imm offset
     */

    private void Ldm(uint instruction)
    {
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var forcePsrOrUser = (instruction & 0x00400000) != 0; //bit 22
        var isWriteback = (instruction & 0x00200000) != 0;
        var rn = (int)(instruction >> 16) & 0xf;

        var registerList = (ushort)(instruction & 0xffff);
        int count = BitOperations.PopCount(registerList);
        uint bytes = (uint)(count * 4);
        bytes = bytes == 0 ? 0x40 : bytes; //if zero transfer count act like full transfer 0x40

        uint baseAddress = rn == 15 ? Registers.ProgramCounter + 4 : Registers[rn];
        uint startAddress, finalAddress;
        if (isUp)
        {
            finalAddress = baseAddress + bytes;
            startAddress = isPreIndex ? baseAddress + 4 : baseAddress;
        }
        else
        {
            finalAddress = baseAddress - bytes;
            startAddress = isPreIndex
                ? baseAddress - bytes
                : baseAddress - bytes + 4;
        }

        if (count == 0)
        {
            Registers[rn] = isUp ? Registers[rn] + 0x40 : Registers[rn] - 0x40;

            Registers[15] = _bus.Read32(startAddress & ~3u);

            _cycles += _bus.GetCpuAccessCycles(startAddress, AccessWidth.Word, sequential: false);
            return;
        }

        var currentMode = Registers.Cpsr.Mode;
        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15)) // system banked registers if r15 not in list and S=1
        {
            //TODO Mode
            Registers.Cpsr.Mode = CpuMode.System;
        }

        uint address = startAddress;
        for (int tReg = 0; tReg < 16; tReg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, tReg);
            if (!shouldTransfer)
                continue;

            uint value = _bus.Read32(address & ~3u);

            if (tReg == 15)
            {
                //word align program counter
                Registers.ProgramCounter = value & ~3u;
            }
            else
            {
                Registers[tReg] = value;
            }

            _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word,
                sequential: tReg != BitOperations.TrailingZeroCount(instruction)); //First transfer N, the rest are S

            address += 4;
        }

        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15))
        {
            //TODO mode
            Registers.Cpsr.Mode = currentMode;
        }

        if (isWriteback && !BitUtils.IsBitSet(instruction, rn)) // no writeback for ldm if rn is in Rlist
        {
            Registers[rn] = finalAddress;
        }

        if (BitUtils.IsBitSet(instruction, 15))
        {
            //ldm refill pipeline if r15 in rList
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Stm(uint instruction)
    {
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var forcePsrOrUser = (instruction & 0x00400000) != 0; //bit 22
        var isWriteback = (instruction & 0x00200000) != 0;
        var rn = (int)(instruction >> 16) & 0xf;

        var registerList = (ushort)(instruction & 0xFFFF);

        int count = BitOperations.PopCount(registerList);
        uint bytes = (uint)(count * 4);
        bytes = bytes == 0 ? 0x40 : bytes; //if zero transfer count act like full transfer 0x40

        uint baseAddress = rn == 15 ? Registers.ProgramCounter + 4 : Registers[rn];

        uint startAddress, finalAddress;
        if (isUp)
        {
            finalAddress = baseAddress + bytes;
            startAddress = isPreIndex ? baseAddress + 4 : baseAddress;
        }
        else
        {
            finalAddress = baseAddress - bytes;
            startAddress = isPreIndex
                ? baseAddress - bytes
                : baseAddress - bytes + 4;
        }

        if (count == 0)
        {
            Registers[rn] = isUp ? Registers[rn] + 0x40 : Registers[rn] - 0x40;

            _bus.Write32(startAddress, Registers[15] + 8);

            _cycles += _bus.GetCpuAccessCycles(startAddress, AccessWidth.Word, sequential: false);
            return;
        }

        var currentMode = Registers.Cpsr.Mode;
        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15)) // system banked registers if r15 not in list and S=1
        {
            //TODO Mode
            Registers.Cpsr.Mode = CpuMode.System;
        }

        uint address = startAddress;

        for (int tReg = 0; tReg < 16; tReg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, tReg);
            if (!shouldTransfer)
                continue;

            if (tReg == rn && tReg != BitOperations.TrailingZeroCount(instruction))
            {
                _bus.Write32(address, finalAddress);
            }
            else
            {
                uint value = tReg == 15
                    ? Registers.ProgramCounter + 8
                    : Registers[tReg];
                _bus.Write32(address, value);
            }

            _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word,
                sequential: tReg != BitOperations.TrailingZeroCount(instruction)); //First transfer N, the rest are S

            address += 4;
        }

        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15))
        {
            //TODO mode
            Registers.Cpsr.Mode = currentMode;
        }

        if (isWriteback)
        {
            Registers[rn] = finalAddress;
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
    }

    private void Swp(uint instruction)
    {
        var rd = (int)(instruction >> 12) & 0xF;
        var rn = (int)(instruction >> 16) & 0xF;
        var rm = (int)instruction & 0xF;

        var address = Registers[rn];

        var temp = _bus.Read32(address);
        _bus.Write32(address, Registers[rm]);
        Registers[rd] = temp;

        _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false) * 2; //2N cycles
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //1S cycle
    }

    private void Swpb(uint instruction)
    {
        var rd = (int)(instruction >> 12) & 0xF;
        var rn = (int)(instruction >> 16) & 0xF;
        var rm = (int)instruction & 0xF;

        var address = Registers[rn];

        var temp = _bus.Read8(address);
        _bus.Write8(address, (byte)(Registers[rm] & 0xFF));
        Registers[rd] = temp;

        _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Byte, sequential: false) * 2; //2N cycles
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //1S cycle
    }

    private void Ldrh(uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        uint loadedValue = _bus.Read16(effectiveAddress);
        loadedValue = BitOperations.RotateRight(loadedValue, (int)((effectiveAddress & 1u) * 8));

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            Registers[rn] = updatedAddress;
        }

        Registers[rd] = loadedValue;
        if (rd == 15)
        {
            Registers.ProgramCounter = loadedValue & ~3u;

            //refill pipeline
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
        }

        _cycles++; //I cycle
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
    }

    private void Ldrsb(uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        var loadedValue = (uint)(sbyte)_bus.Read8(effectiveAddress); //LDRSB

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            Registers[rn] = updatedAddress;
        }

        Registers[rd] = loadedValue;
        if (rd == 15)
        {
            Registers.ProgramCounter = loadedValue & ~3u;

            //refill pipeline
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
        }

        _cycles++; //I cycle
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
    }

    private void Ldrsh(uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        var rawHalfword = _bus.Read16(effectiveAddress);
        var loadedValue = (effectiveAddress & 1) != 0
            ? (uint)BitUtils.SignExtend((rawHalfword >> 8) & 0xff, 8)
            : (uint)BitUtils.SignExtend(rawHalfword, 16);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            Registers[rn] = updatedAddress;
        }

        Registers[rd] = loadedValue;
        if (rd == 15)
        {
            Registers.ProgramCounter = loadedValue & ~3u;

            //refill pipeline
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
        }

        _cycles++; //I cycle
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
    }

    private void Strh(uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        uint value = rd == 15
            ? Registers.ProgramCounter + 4
            : Registers[rd];
        _bus.Write16(effectiveAddress, (ushort)value);
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            Registers[rn] = updatedAddress;
        }
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //STR N
    }
}