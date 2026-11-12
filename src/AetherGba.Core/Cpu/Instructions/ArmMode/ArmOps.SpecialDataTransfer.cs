using System.Numerics;
using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ArmMode;

public static partial class ArmOps
{
    /* SPECIAL DATA TRANSFER INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|1_0_0|P|U|S|W|L|__Rn___|_________Register_List_________| LDM, STM
      |_Cond__|0_0_0_1_0|B|0_0|__Rn___|__Rd___|0_0_0_0|1_0_0_1|__Rm___| SWP, SWPB
      |_Cond__|0_0_0|P|U|0|W|L|__Rn___|__Rd___|0_0_0_0|1|S|H|1|__Rm___| reg offset
      |_Cond__|0_0_0|P|U|1|W|L|__Rn___|__Rd___|_H_Off_|1|S|H|1|_L_Off_| imm offset
     */

    public static void Ldm(Arm7Tdmi cpu, uint instruction)
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

        uint baseAddress = rn == 15 ? cpu.Registers.ProgramCounter + 4 : cpu.Registers[rn];
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
            cpu.Registers[rn] = isUp ? cpu.Registers[rn] + 0x40 : cpu.Registers[rn] - 0x40;

            cpu.Registers[15] = cpu.Bus.Read32(startAddress & ~3u);

            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(startAddress, AccessWidth.Word, sequential: false);
            return;
        }

        var currentMode = cpu.Registers.Cpsr.Mode;
        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15)) // system banked registers if r15 not in list and S=1
        {
            //TODO Mode
            cpu.Registers.Cpsr.Mode = CpuMode.System;
        }

        uint address = startAddress;
        for (int tReg = 0; tReg < 16; tReg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, tReg);
            if (!shouldTransfer)
                continue;

            uint value = cpu.Bus.Read32(address & ~3u);

            if (tReg == 15)
            {
                //word align program counter
                cpu.Registers.ProgramCounter = value & ~3u;
            }
            else
            {
                cpu.Registers[tReg] = value;
            }

            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word,
                sequential: tReg != BitOperations.TrailingZeroCount(instruction)); //First transfer N, the rest are S

            address += 4;
        }

        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15))
        {
            //TODO mode
            cpu.Registers.Cpsr.Mode = currentMode;
        }

        if (isWriteback && !BitUtils.IsBitSet(instruction, rn)) // no writeback for ldm if rn is in Rlist
        {
            cpu.Registers[rn] = finalAddress;
        }

        if (BitUtils.IsBitSet(instruction, 15))
        {
            //ldm refill pipeline if r15 in rList
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Stm(Arm7Tdmi cpu, uint instruction)
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

        uint baseAddress = rn == 15 ? cpu.Registers.ProgramCounter + 4 : cpu.Registers[rn];

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
            cpu.Registers[rn] = isUp ? cpu.Registers[rn] + 0x40 : cpu.Registers[rn] - 0x40;

            cpu.Bus.Write32(startAddress, cpu.Registers[15] + 8);

            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(startAddress, AccessWidth.Word, sequential: false);
            return;
        }

        var currentMode = cpu.Registers.Cpsr.Mode;
        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15)) // system banked registers if r15 not in list and S=1
        {
            //TODO Mode
            cpu.Registers.Cpsr.Mode = CpuMode.System;
        }

        uint address = startAddress;

        for (int tReg = 0; tReg < 16; tReg++)
        {
            var shouldTransfer = BitUtils.IsBitSet(instruction, tReg);
            if (!shouldTransfer)
                continue;

            if (tReg == rn && tReg != BitOperations.TrailingZeroCount(instruction))
            {
                cpu.Bus.Write32(address, finalAddress);
            }
            else
            {
                uint value = tReg == 15
                    ? cpu.Registers.ProgramCounter + 8
                    : cpu.Registers[tReg];
                cpu.Bus.Write32(address, value);
            }

            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word,
                sequential: tReg != BitOperations.TrailingZeroCount(instruction)); //First transfer N, the rest are S

            address += 4;
        }

        if (forcePsrOrUser && !BitUtils.IsBitSet(instruction, 15))
        {
            //TODO mode
            cpu.Registers.Cpsr.Mode = currentMode;
        }

        if (isWriteback)
        {
            cpu.Registers[rn] = finalAddress;
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
    }

    public static void Swp(Arm7Tdmi cpu, uint instruction)
    {
        var rd = (int)(instruction >> 12) & 0xF;
        var rn = (int)(instruction >> 16) & 0xF;
        var rm = (int)instruction & 0xF;

        var address = cpu.Registers[rn];

        var temp = cpu.Bus.Read32(address);
        cpu.Bus.Write32(address, cpu.Registers[rm]);
        cpu.Registers[rd] = temp;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false) * 2; //2N cycles
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //1S cycle
    }

    public static void Swpb(Arm7Tdmi cpu, uint instruction)
    {
        var rd = (int)(instruction >> 12) & 0xF;
        var rn = (int)(instruction >> 16) & 0xF;
        var rm = (int)instruction & 0xF;

        var address = cpu.Registers[rn];

        var temp = cpu.Bus.Read8(address);
        cpu.Bus.Write8(address, (byte)(cpu.Registers[rm] & 0xFF));
        cpu.Registers[rd] = temp;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Byte, sequential: false) * 2; //2N cycles
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //1S cycle
    }

    public static void Ldrh(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : cpu.Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        uint loadedValue = cpu.Bus.Read16(effectiveAddress);
        loadedValue = BitOperations.RotateRight(loadedValue, (int)((effectiveAddress & 1u) * 8));

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            cpu.Registers[rn] = updatedAddress;
        }

        cpu.Registers[rd] = loadedValue;
        if (rd == 15)
        {
            cpu.Registers.ProgramCounter = loadedValue & ~3u;

            //refill pipeline
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
        }

        cpu.Cycles++; //I cycle
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
    }

    public static void Ldrsb(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : cpu.Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        var loadedValue = (uint)(sbyte)cpu.Bus.Read8(effectiveAddress); //LDRSB

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            cpu.Registers[rn] = updatedAddress;
        }

        cpu.Registers[rd] = loadedValue;
        if (rd == 15)
        {
            cpu.Registers.ProgramCounter = loadedValue & ~3u;

            //refill pipeline
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
        }

        cpu.Cycles++; //I cycle
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
    }

    public static void Ldrsh(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : cpu.Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        var rawHalfword = cpu.Bus.Read16(effectiveAddress);
        var loadedValue = (effectiveAddress & 1) != 0
            ? (uint)BitUtils.SignExtend((rawHalfword >> 8) & 0xff, 8)
            : (uint)BitUtils.SignExtend(rawHalfword, 16);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Halfword, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            cpu.Registers[rn] = updatedAddress;
        }

        cpu.Registers[rd] = loadedValue;
        if (rd == 15)
        {
            cpu.Registers.ProgramCounter = loadedValue & ~3u;

            //refill pipeline
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
        }

        cpu.Cycles++; //I cycle
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true); //S cycle
    }

    public static void Strh(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)(instruction >> 16) & 0xF;
        var baseAddress = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var rd = (int)(instruction >> 12) & 0xF;
        var isWriteback = (instruction & 0x00200000) != 0; //bit 21
        var immediate = (instruction & 0x00400000) != 0; //bit 22
        var isUp = (instruction & 0x00800000) != 0; //bit 23
        var isPreIndex = (instruction & 0x01000000) != 0; //bit 24

        var immOffset = ((instruction >> 4) & 0xF0) | (instruction & 0x0F);
        var rm = (int)instruction & 0x0F;
        var offset = immediate ? immOffset : cpu.Registers[rm];

        var updatedAddress = isUp
            ? baseAddress + offset
            : baseAddress - offset;
        var effectiveAddress = isPreIndex
            ? updatedAddress
            : baseAddress;

        uint value = rd == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rd];
        cpu.Bus.Write16(effectiveAddress, (ushort)value);
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N cycle

        if (isPreIndex && isWriteback || !isPreIndex)
        {
            cpu.Registers[rn] = updatedAddress;
        }
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false); //STR N
    }
}