using AetherGba.Core.Common;
using AetherGba.Core.Dma;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Timers;

namespace AetherGba.Core.Memory;

public sealed class IoRegisters
{
    public uint ReadIo32Aligned(uint address)
    {
        if ((address & 3u) != 0)
        {
            Console.WriteLine("ReadIo32Aligned called with non word aligned address");
            //throw new InvalidOperationException("ReadIo32Aligned called with non word aligned address");
        }

        var lo = GetMappedRegister(address);
        var hi = GetMappedRegister(address + 2);
        return (uint)((hi << 16) | lo);
    }

    public ushort ReadIo16Aligned(uint address)
    {
        if ((address & 1u) != 0)
        {
            Console.WriteLine("ReadIo16Aligned called with non halfword aligned address");
            //throw new InvalidOperationException("ReadIo16Aligned called with non halfword aligned address");
        }

        return GetMappedRegister(address);
    }

    public byte ReadIo8(uint address)
    {
        var aligned = address & ~1u;
        var registerValue = GetMappedRegister(aligned);
        //LSByte if aligned, MSByte if unaligned
        return (byte)((registerValue >> ((int)(address & 1) * 8)) & 0xFF);
    }

    public void WriteIo32Aligned(uint address, uint value)
    {
        if ((address & 3u) != 0)
        {
            Console.WriteLine("WriteIo32Aligned called with non word aligned address");
            //throw new InvalidOperationException("WriteIo32Aligned called with non word aligned address");
        }

        WriteIo16Aligned(address, (ushort)value);
        WriteIo16Aligned(address + 2, (ushort)(value >> 16));
    }

    public void WriteIo8(uint address, byte value)
    {
        var aligned = address & ~1u;
        var existingValue = GetMappedRegister(aligned);
        var shift = (int)(address & 1) * 8;
        var merged = (ushort)((existingValue & ~(0xFF << shift)) | (value << shift));
        WriteIo16Aligned(aligned, merged);
    }

    public void WriteIo16Aligned(uint address, ushort value)
    {
        if ((address & 1u) != 0)
        {
            Console.WriteLine("WriteIo16Aligned called with non halfword aligned address");
            //throw new InvalidOperationException("WriteIo16Aligned called with non halfword aligned address");
        }

        switch (address)
        {
            //TODO: watch readonly
            #region Display
            case 0x04000000:
                REG_DISPCNT = value;
                break;
            case 0x04000004:
                REG_DISPSTAT = (ushort)((REG_DISPSTAT & 0x0007) | (value & 0xfff8));
                break;
            case 0x04000006:
                REG_VCOUNT = value;
                break;
            case 0x04000008:
                //bit 15 forced low for bg 0 and 1
                REG_BG0CNT = (ushort)(value & 0xdfff);
                break;
            case 0x0400000A:
                //bit 15 forced low for bg 0 and 1
                REG_BG1CNT = (ushort)(value & 0xdfff);
                break;
            case 0x0400000C:
                REG_BG2CNT = value;
                break;
            case 0x0400000E:
                REG_BG3CNT = value;
                break;
            case 0x04000010:
                REG_BG0HOFS = (ushort)(value & 0x1ff);
                break;
            case 0x04000012:
                REG_BG0VOFS = (ushort)(value & 0x1ff);
                break;
            case 0x04000014:
                REG_BG1HOFS = (ushort)(value & 0x1ff);
                break;
            case 0x04000016:
                REG_BG1VOFS = (ushort)(value & 0x1ff);
                break;
            case 0x04000018:
                REG_BG2HOFS = (ushort)(value & 0x1ff);
                break;
            case 0x0400001A:
                REG_BG2VOFS = (ushort)(value & 0x1ff);
                break;
            case 0x0400001C:
                REG_BG3HOFS = (ushort)(value & 0x1ff);
                break;
            case 0x0400001E:
                REG_BG3VOFS = (ushort)(value & 0x1ff);
                break;
            case 0x04000020:
                REG_BG2PA = value;
                break;
            case 0x04000022:
                REG_BG2PB = value;
                break;
            case 0x04000024:
                REG_BG2PC = value;
                break;
            case 0x04000026:
                REG_BG2PD = value;
                break;
            case 0x04000028:
                REG_BG2X = (REG_BG2X & 0x0FFF0000u) | value; //set low bytes
                InternalBg2X = BitUtils.SignExtend((int)REG_BG2X, 28);
                break;
            case 0x0400002A:
                REG_BG2X = ((REG_BG2X & 0xffff) | ((value & 0x0fffu) << 16)); //set high bytes and ignore bit 28-31
                InternalBg2X = BitUtils.SignExtend((int)REG_BG2X, 28);
                break;
            case 0x0400002C:
                REG_BG2Y = (REG_BG2Y & 0x0FFF0000) | value; //set low bytes
                InternalBg2Y = BitUtils.SignExtend((int)REG_BG2Y, 28);
                break;
            case 0x0400002E:
                REG_BG2Y = ((REG_BG2Y & 0xffff) | ((value & 0x0fffu) << 16)); //set high bytes and ignore bit 28-31
                InternalBg2Y = BitUtils.SignExtend((int)REG_BG2Y, 28);
                break;
            case 0x04000030:
                REG_BG3PA = value;
                break;
            case 0x04000032:
                REG_BG3PB = value;
                break;
            case 0x04000034:
                REG_BG3PC = value;
                break;
            case 0x04000036:
                REG_BG3PD = value;
                break;
            case 0x04000038:
                REG_BG3X = (REG_BG3X & 0x0FFF0000u) | value; //set low bytes
                InternalBg3X = BitUtils.SignExtend((int)REG_BG3X, 28);
                break;
            case 0x0400003A:
                REG_BG3X = ((REG_BG3X & 0xffff) | ((value & 0x0fffu) << 16)); //set high bytes and ignore bit 28-31
                InternalBg3X = BitUtils.SignExtend((int)REG_BG3X, 28);
                break;
            case 0x0400003C:
                REG_BG3Y = (REG_BG3Y & 0x0FFF0000) | value; //set low bytes
                InternalBg3Y = BitUtils.SignExtend((int)REG_BG3Y, 28);
                break;
            case 0x0400003E:
                REG_BG3Y = ((REG_BG3Y & 0xffff) | ((value & 0x0fffu) << 16)); //set high bytes and ignore bit 28-31
                InternalBg3Y = BitUtils.SignExtend((int)REG_BG3Y, 28);
                break;
            case 0x04000040:
                REG_WIN0H = value;
                break;
            case 0x04000042:
                REG_WIN1H = value;
                break;
            case 0x04000044:
                REG_WIN0V = value;
                break;
            case 0x04000046:
                REG_WIN1V = value;
                break;
            case 0x04000048:
                REG_WININ = value;
                break;
            case 0x0400004A:
                REG_WINOUT = value;
                break;
            case 0x0400004C:
                REG_MOSAIC = value;
                break;
            case 0x04000050:
                REG_BLDCNT = value;
                break;
            case 0x04000052:
                REG_BLDALPHA = value;
                break;
            case 0x04000054:
                REG_BLDY = value;
                break;
            #endregion
            #region Sound
            case 0x04000060:
                REG_SOUND1CNT_L = value;
                break;
            case 0x04000062:
                REG_SOUND1CNT_H = value;
                break;
            case 0x04000064:
                REG_SOUND1CNT_X = value;
                break;
            case 0x04000068:
                REG_SOUND2CNT_L = value;
                break;
            case 0x0400006c:
                REG_SOUND2CNT_H = value;
                break;
            case 0x04000070:
                REG_SOUND3CNT_L = value;
                break;
            case 0x04000072:
                REG_SOUND3CNT_H = value;
                break;
            case 0x04000074:
                REG_SOUND3CNT_X = value;
                break;
            case 0x04000078:
                REG_SOUND4CNT_L = value;
                break;
            case 0x0400007c:
                REG_SOUND4CNT_H = value;
                break;
            case 0x04000080:
                REG_SOUNDCNT_L = value;
                break;
            case 0x04000082:
                REG_SOUNDCNT_H = value;
                break;
            case 0x04000084:
                REG_SOUNDCNT_X = value;
                break;
            case 0x04000088:
                REG_SOUNDBIAS = value;
                break;
            case 0x04000090:
                REG_WAVE_RAM0_L = value;
                break;
            case 0x04000092:
                REG_WAVE_RAM0_H = value;
                break;
            case 0x04000094:
                REG_WAVE_RAM1_L = value;
                break;
            case 0x04000096:
                REG_WAVE_RAM1_H = value;
                break;
            case 0x04000098:
                REG_WAVE_RAM2_L = value;
                break;
            case 0x0400009a:
                REG_WAVE_RAM2_H = value;
                break;
            case 0x0400009c:
                REG_WAVE_RAM3_L = value;
                break;
            case 0x0400009e:
                REG_WAVE_RAM3_H = value;
                break;
            case 0x040000a0:
                REG_FIFO_A = (REG_FIFO_A & 0xffff0000) | value;
                break;
            case 0x040000a2:
                REG_FIFO_A = (REG_FIFO_A & 0xffff) | ((uint)value << 16);
                break;
            case 0x040000a4:
                REG_FIFO_B = (REG_FIFO_A & 0xffff0000) | value;
                break;
            case 0x040000a6:
                REG_FIFO_B = (REG_FIFO_A & 0xffff) | ((uint)value << 16);
                break;
            #endregion
            #region Dma
            case 0x040000B0:
                REG_DMA0SAD = (REG_DMA0SAD & 0xFFFF0000u) | value;
                break;
            case 0x040000B2:
                REG_DMA0SAD = (REG_DMA0SAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000B4:
                REG_DMA0DAD = (REG_DMA0DAD & 0xFFFF0000u) | value;
                break;
            case 0x040000B6:
                REG_DMA0DAD = (REG_DMA0DAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000B8:
                REG_DMA0CNT_L = value == 0 ? (ushort)0x4000 : value;
                break;
            case 0x040000BA:
                REG_DMA0CNT_H = value;
                if ((value & 0x8000) != 0)
                {
                    _dma.Channels[0].Enable();
                }
                else
                {
                    _dma.Channels[0].Enabled = false;
                }
                if ((value & 0x8000) != 0)
                {
                    //RunDmas(DmaTimingType.Immediately, bus);
                }
                break;
            case 0x040000BC:
                REG_DMA1SAD = (REG_DMA1SAD & 0xFFFF0000u) | value;
                break;
            case 0x040000BE:
                REG_DMA1SAD = (REG_DMA1SAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000C0:
                REG_DMA1DAD = (REG_DMA1DAD & 0xFFFF0000u) | value;
                break;
            case 0x040000C2:
                REG_DMA1DAD = (REG_DMA1DAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000C4:
                REG_DMA1CNT_L = value == 0 ? (ushort)0x4000 : value;
                break;
            case 0x040000C6:
                REG_DMA1CNT_H = value;
                if ((value & 0x8000) != 0)
                {
                    _dma.Channels[1].Enable();
                }
                else
                {
                    _dma.Channels[1].Enabled = false;
                }
                if ((value & 0x8000) != 0)
                {
                    //RunDmas(DmaTimingType.Immediately, bus);
                }
                break;
            case 0x040000C8:
                REG_DMA2SAD = (REG_DMA2SAD & 0xFFFF0000u) | value;
                break;
            case 0x040000CA:
                REG_DMA2SAD = (REG_DMA2SAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000CC:
                REG_DMA2DAD = (REG_DMA2DAD & 0xFFFF0000u) | value;
                break;
            case 0x040000CE:
                REG_DMA2DAD = (REG_DMA2DAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000D0:
                REG_DMA2CNT_L = value == 0 ? (ushort)0x4000 : value;
                break;
            case 0x040000D2:
                REG_DMA2CNT_H = value;
                if ((value & 0x8000) != 0)
                {
                    _dma.Channels[2].Enable();
                }
                else
                {
                    _dma.Channels[2].Enabled = false;
                }
                if ((value & 0x8000) != 0)
                {
                    //RunDmas(DmaTimingType.Immediately, bus);
                }
                break;
            case 0x040000D4:
                REG_DMA3SAD = (REG_DMA3SAD & 0xFFFF0000u) | value;
                break;
            case 0x040000D6:
                REG_DMA3SAD = (REG_DMA3SAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000D8:
                REG_DMA3DAD = (REG_DMA3DAD & 0xFFFF0000u) | value;
                break;
            case 0x040000DA:
                REG_DMA3DAD = (REG_DMA3DAD & 0x0000FFFFu) | (uint)(value << 16);
                break;
            case 0x040000DC:
                REG_DMA3CNT_L = value;
                break;
            case 0x040000DE:
                REG_DMA3CNT_H = value;
                if ((value & 0x8000) != 0)
                {
                    _dma.Channels[3].Enable();
                }
                else
                {
                    _dma.Channels[3].Enabled = false;
                }
                if ((value & 0x8000) != 0)
                {
                    //RunDmas(DmaTimingType.Immediately, bus);
                }
                break;
            #endregion
            #region Timers
            case 0x04000100:
                _timerController.WriteReload(0, value);
                break;
            case 0x04000102:
                _timerController.WriteControl(0, value);
                break;
            case 0x04000104:
                _timerController.WriteReload(1, value);
                break;
            case 0x04000106:
                _timerController.WriteControl(1, value);
                break;
            case 0x04000108:
                _timerController.WriteReload(2, value);
                break;
            case 0x0400010A:
                _timerController.WriteControl(2, value);
                break;
            case 0x0400010C:
                _timerController.WriteReload(3, value);
                break;
            case 0x0400010E:
                _timerController.WriteControl(3, value);
                break;
            #endregion
            #region Serial Communication (1)
            case 0x04000120:
                REG_SCD0 = value;
                break;
            case 0x04000122:
                REG_SCD1 = value;
                break;
            case 0x04000124:
                REG_SCD2 = value;
                break;
            case 0x04000126:
                REG_SCD3 = value;
                break;
            case 0x04000128:
                REG_SCCNT_L = value;
                break;
            case 0x0400012a:
                REG_SCCNT_H = value;
                break;
            #endregion
            #region Keypad
            case 0x04000132:
                REG_KEYCNT = value;
                break;
            #endregion
            #region Serial Communication (2)
            case 0x04000134:
                REG_RCNT = value;
                break;
            case 0x04000140:
                REG_JOYCNT = value;
                break;
            case 0x04000150:
                REG_JOY_RECV = value; //shift later
                break;
            case 0x04000152:
                REG_JOY_RECV = value; //shift later
                break;
            case 0x04000154:
                REG_JOY_TRANS = value; //shift later
                break;
            case 0x04000156:
                REG_JOY_TRANS = value; //shift later
                break;
            case 0x04000158:
                REG_JOYSTAT = value; //shift later
                break;
            #endregion
            #region Interrupts
            case 0x04000200:
                REG_IE = value;
                _interruptController.UpdateServiceIrq();
                break;
            case 0x04000202:
                REG_IF = (ushort)(REG_IF & ~value);
                _interruptController.UpdateServiceIrq();
                break;
            case 0x04000208:
                REG_IME = (value & 1) != 0;
                _interruptController.UpdateServiceIrq();
                break;
            #endregion
            #region Cartridge and System Control
            case 0x04000204:
                REG_WAITCNT = (ushort)(value & 0x7FFF);
                break;
            #endregion
            default:
                //Console.WriteLine($"Unmapped IO write at Address:{address:x8}, Value:{value:x8}");
                break;
        }
    }

    private ushort GetMappedRegister(uint address) =>
        address switch
        {
            //TODO: watch writeonly
            #region Display
            0x04000000 => REG_DISPCNT,
            0x04000004 => REG_DISPSTAT,
            0x04000006 => REG_VCOUNT,
            0x04000008 => REG_BG0CNT,
            0x0400000A => REG_BG1CNT,
            0x0400000C => REG_BG2CNT,
            0x0400000E => REG_BG3CNT,
            0x04000010 => REG_BG0HOFS,
            0x04000012 => REG_BG0VOFS,
            0x04000014 => REG_BG1HOFS,
            0x04000016 => REG_BG1VOFS,
            0x04000018 => REG_BG2HOFS,
            0x0400001A => REG_BG2VOFS,
            0x0400001C => REG_BG3HOFS,
            0x0400001E => REG_BG3VOFS,
            0x04000020 => REG_BG2PA,
            0x04000022 => REG_BG2PB,
            0x04000024 => REG_BG2PC,
            0x04000026 => REG_BG2PD,
            0x04000028 => (ushort)REG_BG2X,
            0x0400002A => (ushort)(REG_BG2X >> 16),
            0x0400002C => (ushort)REG_BG2Y,
            0x0400002E => (ushort)(REG_BG2Y >> 16),
            0x04000030 => REG_BG3PA,
            0x04000032 => REG_BG3PB,
            0x04000034 => REG_BG3PC,
            0x04000036 => REG_BG3PD,
            0x04000038 => (ushort)REG_BG3X,
            0x0400003A => (ushort)(REG_BG3X >> 16),
            0x0400003C => (ushort)REG_BG3Y,
            0x0400003E => (ushort)(REG_BG3Y >> 16),
            0x04000040 => REG_WIN0H,
            0x04000042 => REG_WIN1H,
            0x04000044 => REG_WIN0V,
            0x04000046 => REG_WIN1V,
            0x04000048 => REG_WININ,
            0x0400004A => REG_WINOUT,
            0x0400004C => REG_MOSAIC,
            0x04000050 => REG_BLDCNT,
            0x04000052 => REG_BLDALPHA,
            0x04000054 => REG_BLDY,
            #endregion
            #region Sound
            0x04000060 => REG_SOUND1CNT_L,
            0x04000062 => REG_SOUND1CNT_H,
            0x04000064 => REG_SOUND1CNT_X,
            0x04000068 => REG_SOUND2CNT_L,
            0x0400006C => REG_SOUND2CNT_H,
            0x04000070 => REG_SOUND3CNT_L,
            0x04000072 => REG_SOUND3CNT_H,
            0x04000074 => REG_SOUND3CNT_X,
            0x04000078 => REG_SOUND4CNT_L,
            0x0400007c => REG_SOUND4CNT_H,
            0x04000080 => REG_SOUNDCNT_L,
            0x04000082 => REG_SOUNDCNT_H,
            0x04000084 => REG_SOUNDCNT_X,
            0x04000088 => REG_SOUNDBIAS,
            0x04000090 => REG_WAVE_RAM0_L,
            0x04000092 => REG_WAVE_RAM0_H,
            0x04000094 => REG_WAVE_RAM1_L,
            0x04000096 => REG_WAVE_RAM1_H,
            0x04000098 => REG_WAVE_RAM2_L,
            0x0400009a => REG_WAVE_RAM2_H,
            0x0400009c => REG_WAVE_RAM3_L,
            0x0400009e => REG_WAVE_RAM3_H,
            0x040000a0 => (ushort)REG_FIFO_A,
            0x040000a2 => (ushort)(REG_FIFO_A >> 16),
            0x040000a4 => (ushort)REG_FIFO_B,
            0x040000a6 => (ushort)(REG_FIFO_B >> 16),
            #endregion
            #region Dma
            0x040000B0 => (ushort)REG_DMA0SAD,
            0x040000B2 => (ushort)(REG_DMA0SAD >> 16),
            0x040000B4 => (ushort)REG_DMA0DAD,
            0x040000B6 => (ushort)(REG_DMA0DAD >> 16),
            0x040000B8 => REG_DMA0CNT_L,
            0x040000BA => REG_DMA0CNT_H,
            0x040000BC => (ushort)REG_DMA1SAD,
            0x040000Be => (ushort)(REG_DMA1SAD >> 16),
            0x040000C0 => (ushort)REG_DMA1DAD,
            0x040000C2 => (ushort)(REG_DMA1DAD >> 16),
            0x040000C4 => REG_DMA1CNT_L,
            0x040000C6 => REG_DMA1CNT_H,
            0x040000C8 => (ushort)REG_DMA2SAD,
            0x040000Ca => (ushort)(REG_DMA2SAD >> 16),
            0x040000CC => (ushort)REG_DMA2DAD,
            0x040000Ce => (ushort)(REG_DMA2DAD >> 16),
            0x040000D0 => REG_DMA2CNT_L,
            0x040000D2 => REG_DMA2CNT_H,
            0x040000D4 => (ushort)REG_DMA3SAD,
            0x040000D6 => (ushort)(REG_DMA3SAD >> 16),
            0x040000D8 => (ushort)REG_DMA3DAD,
            0x040000Da => (ushort)(REG_DMA3DAD >> 16),
            0x040000DC => REG_DMA3CNT_L,
            0x040000DE => REG_DMA3CNT_H,
            #endregion
            #region Timers
            0x04000100 => _timerController.ReadCounter(0),
            0x04000102 => _timerController.ReadControl(0),
            0x04000104 => _timerController.ReadCounter(1),
            0x04000106 => _timerController.ReadControl(1),
            0x04000108 => _timerController.ReadCounter(2),
            0x0400010A => _timerController.ReadControl(2),
            0x0400010C => _timerController.ReadCounter(3),
            0x0400010E => _timerController.ReadControl(3),
            #endregion
            #region Serial Communication (1)
            0x04000120 => REG_SCD0,
            0x04000122 => REG_SCD1,
            0x04000124 => REG_SCD2,
            0x04000126 => REG_SCD3,
            0x04000128 => REG_SCCNT_L,
            0x0400012a => REG_SCCNT_H,
            #endregion
            #region Keypad
            0x04000130 => REG_KEYINPUT,
            0x04000132 => REG_KEYCNT,
            #endregion
            #region Serial Communication (2)
            0x04000134 => REG_RCNT,
            0x04000140 => REG_JOYCNT,
            0x04000150 => (ushort)REG_JOY_RECV, //shift later
            0x04000152 => (ushort)REG_JOY_RECV, //shift later
            0x04000154 => (ushort)REG_JOY_TRANS, //shift later
            0x04000156 => (ushort)REG_JOY_TRANS, //shift later
            0x04000158 => REG_JOYSTAT,
            #endregion
            #region Interrupts
            0x04000200 => REG_IE,
            0x04000202 => REG_IF,
            0x04000208 => (ushort)(REG_IME ? 1 : 0),
            #endregion
            #region Cartridge and System Control
            0x04000204 => REG_WAITCNT,
            #endregion
            _ => 0 //TODO: add openBus behavior
        };

    #region Display

    /// <summary>
    /// 0x04000000
    /// </summary>
    public ushort REG_DISPCNT { get; set; }
    /// <summary>
    /// 0x04000004
    /// </summary>
    public ushort REG_DISPSTAT { get; set; }
    /// <summary>
    /// 0x04000006
    /// </summary>
    public ushort REG_VCOUNT { get; set; }
    /// <summary>
    /// 0x04000008
    /// </summary>
    public ushort REG_BG0CNT { get; set; }
    /// <summary>
    /// 0x0400000A
    /// </summary>
    public ushort REG_BG1CNT { get; set; }
    /// <summary>
    /// 0x0400000C
    /// </summary>
    public ushort REG_BG2CNT { get; set; }
    /// <summary>
    /// 0x0400000E
    /// </summary>
    public ushort REG_BG3CNT { get; set; }
    /// <summary>
    /// 0x04000010
    /// </summary>
    public ushort REG_BG0HOFS { get; set; }
    /// <summary>
    /// 0x04000012
    /// </summary>
    public ushort REG_BG0VOFS { get; set; }
    /// <summary>
    /// 0x04000014
    /// </summary>
    public ushort REG_BG1HOFS { get; set; }
    /// <summary>
    /// 0x04000016
    /// </summary>
    public ushort REG_BG1VOFS { get; set; }
    /// <summary>
    /// 0x04000018
    /// </summary>
    public ushort REG_BG2HOFS { get; set; }
    /// <summary>
    /// 0x0400001A
    /// </summary>
    public ushort REG_BG2VOFS { get; set; }
    /// <summary>
    /// 0x0400001C
    /// </summary>
    public ushort REG_BG3HOFS { get; set; }
    /// <summary>
    /// 0x0400001E
    /// </summary>
    public ushort REG_BG3VOFS { get; set; }
    /// <summary>
    /// 0x04000020
    /// </summary>
    public ushort REG_BG2PA { get; set; } = 0x0100;
    /// <summary>
    /// 0x04000022
    /// </summary>
    public ushort REG_BG2PB { get; set; }
    /// <summary>
    /// 0x04000024
    /// </summary>
    public ushort REG_BG2PC { get; set; }
    /// <summary>
    /// 0x04000026
    /// </summary>
    public ushort REG_BG2PD { get; set; } = 0x0100;
    /// <summary>
    /// 0x04000028
    /// </summary>
    public uint REG_BG2X { get; set; }
    public int InternalBg2X { get; set; }
    /// <summary>
    /// 0x0400002C
    /// </summary>
    public uint REG_BG2Y { get; set; }
    public int InternalBg2Y { get; set; }
    /// <summary>
    /// 0x04000030
    /// </summary>
    public ushort REG_BG3PA { get; set; } = 0x0100;
    /// <summary>
    /// 0x04000032
    /// </summary>
    public ushort REG_BG3PB { get; set; }
    /// <summary>
    /// 0x04000034
    /// </summary>
    public ushort REG_BG3PC { get; set; }
    /// <summary>
    /// 0x04000036
    /// </summary>
    public ushort REG_BG3PD { get; set; } = 0x0100;
    /// <summary>
    /// 0x04000038
    /// </summary>
    public uint REG_BG3X { get; set; }
    public int InternalBg3X { get; set; }
    /// <summary>
    /// 0x0400003C
    /// </summary>
    public uint REG_BG3Y { get; set; }
    public int InternalBg3Y { get; set; }
    /// <summary>
    /// 0x04000040
    /// </summary>
    public ushort REG_WIN0H { get; set; }
    /// <summary>
    /// 0x04000042
    /// </summary>
    public ushort REG_WIN1H { get; set; }
    /// <summary>
    /// 0x04000044
    /// </summary>
    public ushort REG_WIN0V { get; set; }
    /// <summary>
    /// 0x04000046
    /// </summary>
    public ushort REG_WIN1V { get; set; }
    /// <summary>
    /// 0x04000048
    /// </summary>
    public ushort REG_WININ { get; set; }
    /// <summary>
    /// 0x0400004A
    /// </summary>
    public ushort REG_WINOUT { get; set; }
    /// <summary>
    /// 0x0400004C
    /// </summary>
    public ushort REG_MOSAIC { get; set; }
    /// <summary>
    /// 0x04000050
    /// </summary>
    public ushort REG_BLDCNT { get; set; }
    /// <summary>
    /// 0x04000052
    /// </summary>
    public ushort REG_BLDALPHA { get; set; }
    /// <summary>
    /// 0x04000054
    /// </summary>
    public ushort REG_BLDY { get; set; }

    #endregion

    #region Sound

    /// <summary>
    /// 0x04000060
    /// </summary>
    public ushort REG_SOUND1CNT_L { get; set; }
    /// <summary>
    /// 0x04000062
    /// </summary>
    public ushort REG_SOUND1CNT_H { get; set; }
    /// <summary>
    /// 0x04000064
    /// </summary>
    public ushort REG_SOUND1CNT_X { get; set; }
    /// <summary>
    /// 0x04000068
    /// </summary>
    public ushort REG_SOUND2CNT_L { get; set; }
    /// <summary>
    /// 0x0400006C
    /// </summary>
    public ushort REG_SOUND2CNT_H { get; set; }
    /// <summary>
    /// 0x04000070
    /// </summary>
    public ushort REG_SOUND3CNT_L { get; set; }
    /// <summary>
    /// 0x04000072
    /// </summary>
    public ushort REG_SOUND3CNT_H { get; set; }
    /// <summary>
    /// 0x04000074
    /// </summary>
    public ushort REG_SOUND3CNT_X { get; set; }
    /// <summary>
    /// 0x04000078
    /// </summary>
    public ushort REG_SOUND4CNT_L { get; set; }
    /// <summary>
    /// 0x0400007C
    /// </summary>
    public ushort REG_SOUND4CNT_H { get; set; }
    /// <summary>
    /// 0x04000080
    /// </summary>
    public ushort REG_SOUNDCNT_L { get; set; }
    /// <summary>
    /// 0x04000082
    /// </summary>
    public ushort REG_SOUNDCNT_H { get; set; }
    /// <summary>
    /// 0x04000084
    /// </summary>
    public ushort REG_SOUNDCNT_X { get; set; }
    /// <summary>
    /// 0x04000088
    /// </summary>
    public ushort REG_SOUNDBIAS { get; set; }
    /// <summary>
    /// 0x04000090
    /// </summary>
    public ushort REG_WAVE_RAM0_L { get; set; }
    /// <summary>
    /// 0x04000092
    /// </summary>
    public ushort REG_WAVE_RAM0_H { get; set; }
    /// <summary>
    /// 0x04000094
    /// </summary>
    public ushort REG_WAVE_RAM1_L { get; set; }
    /// <summary>
    /// 0x04000096
    /// </summary>
    public ushort REG_WAVE_RAM1_H { get; set; }
    /// <summary>
    /// 0x04000098
    /// </summary>
    public ushort REG_WAVE_RAM2_L { get; set; }
    /// <summary>
    /// 0x0400009a
    /// </summary>
    public ushort REG_WAVE_RAM2_H { get; set; }
    /// <summary>
    /// 0x0400009c
    /// </summary>
    public ushort REG_WAVE_RAM3_L { get; set; }
    /// <summary>
    /// 0x0400009e
    /// </summary>
    public ushort REG_WAVE_RAM3_H { get; set; }
    /// <summary>
    /// 0x040000A0
    /// </summary>
    public uint REG_FIFO_A { get; set; }
    /// <summary>
    /// 0x040000A4
    /// </summary>
    public uint REG_FIFO_B { get; set; }
    #endregion

    #region Dma

    private DmaController _dma = null!;
    public void ConnectDmaController(DmaController dmaController)
    {
        _dma = dmaController;
    }

    /// <summary>
    /// 0x040000B0
    /// </summary>
    public uint REG_DMA0SAD { get; set; }
    /// <summary>
    /// 0x040000B4
    /// </summary>
    public uint REG_DMA0DAD { get; set; }
    /// <summary>
    /// 0x040000B8
    /// </summary>
    public ushort REG_DMA0CNT_L { get; set; }
    /// <summary>
    /// 0x040000BA
    /// </summary>
    public ushort REG_DMA0CNT_H { get; set; }
    /// <summary>
    /// 0x040000BC
    /// </summary>
    public uint REG_DMA1SAD { get; set; }
    /// <summary>
    /// 0x040000C0
    /// </summary>
    public uint REG_DMA1DAD { get; set; }
    /// <summary>
    /// 0x040000C4
    /// </summary>
    public ushort REG_DMA1CNT_L { get; set; }
    /// <summary>
    /// 0x040000C6
    /// </summary>
    public ushort REG_DMA1CNT_H { get; set; }
    /// <summary>
    /// 0x040000C8
    /// </summary>
    public uint REG_DMA2SAD { get; set; }
    /// <summary>
    /// 0x040000CC
    /// </summary>
    public uint REG_DMA2DAD { get; set; }
    /// <summary>
    /// 0x040000D0
    /// </summary>
    public ushort REG_DMA2CNT_L { get; set; }
    /// <summary>
    /// 0x040000D2
    /// </summary>
    public ushort REG_DMA2CNT_H { get; set; }
    /// <summary>
    /// 0x040000D4
    /// </summary>
    public uint REG_DMA3SAD { get; set; }
    /// <summary>
    /// 0x040000D8
    /// </summary>
    public uint REG_DMA3DAD { get; set; }
    /// <summary>
    /// 0x040000DC
    /// </summary>
    public ushort REG_DMA3CNT_L { get; set; }
    /// <summary>
    /// 0x040000DE
    /// </summary>
    public ushort REG_DMA3CNT_H { get; set; }
    #endregion

    #region Timers

    private TimerController _timerController = null!;
    public void ConnectTimerController(TimerController timerController)
    {
        _timerController = timerController;
    }
    #endregion

    #region Serial Communication (1)

    /// <summary>
    /// 0x04000120
    /// </summary>
    public ushort REG_SCD0 { get; set; }
    /// <summary>
    /// 0x04000122
    /// </summary>
    public ushort REG_SCD1 { get; set; }
    /// <summary>
    /// 0x04000124
    /// </summary>
    public ushort REG_SCD2 { get; set; }
    /// <summary>
    /// 0x04000126
    /// </summary>
    public ushort REG_SCD3 { get; set; }
    /// <summary>
    /// 0x04000128
    /// </summary>
    public ushort REG_SCCNT_L { get; set; }
    /// <summary>
    /// 0x0400012a
    /// </summary>
    public ushort REG_SCCNT_H { get; set; }
    #endregion

    #region Keypad

    /// <summary>
    /// 0x04000130
    /// </summary>
    public ushort REG_KEYINPUT { get; set; } = 0x3ff;
    /// <summary>
    /// 0x04000132
    /// </summary>
    public ushort REG_KEYCNT { get; set; }
    #endregion

    #region Serial Communication (2)

    /// <summary>
    /// 0x04000134
    /// </summary>
    public ushort REG_RCNT { get; set; }
    /// <summary>
    /// 0x04000140
    /// </summary>
    public ushort REG_JOYCNT { get; set; }
    /// <summary>
    /// 0x04000150
    /// </summary>
    public uint REG_JOY_RECV { get; set; }
    /// <summary>
    /// 0x04000154
    /// </summary>
    public uint REG_JOY_TRANS { get; set; }
    /// <summary>
    /// 0x04000158
    /// </summary>
    public ushort REG_JOYSTAT { get; set; }
    #endregion

    #region Interrupts

    private InterruptController _interruptController = null!;
    public void ConnectInterruptController(InterruptController interruptController)
    {
        _interruptController = interruptController;
    }
    /// <summary>
    /// 0x04000200
    /// </summary>
    public ushort REG_IE { get; set; }
    /// <summary>
    /// 0x04000202
    /// </summary>
    public ushort REG_IF { get; set; }
    /// <summary>
    /// 0x04000208
    /// </summary>
    public bool REG_IME { get; set; }
    #endregion

    #region Cartridge and System Control

    /// <summary>
    /// 0x04000204
    /// </summary>
    public ushort REG_WAITCNT { get; set; }
    /// <summary>
    /// 0x04000300
    /// </summary>
    public ushort REG_HALTCNT { get; set; }
    #endregion
}