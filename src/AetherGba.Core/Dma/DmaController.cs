using AetherGba.Core.Common;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Dma;

public sealed class DmaController
{
    internal readonly DmaChannel[] Channels;
    private readonly InterruptController _interrupts;

    internal DmaController(InterruptController interrupts, GbaMemory memory)
    {
        _interrupts = interrupts;
        Channels = Enumerable.Range(0, 4).Select((_, index) => new DmaChannel(memory, index)).ToArray();
    }

    public void RunDmas(DmaTimingType timingType, GbaBus bus)
    {
        foreach (var channel in Channels)
        {
            if (!channel.Enabled ||
                (DmaTimingType)((channel.Control >> 12) & 0b11) != timingType)
            {
                continue;
            }

            if (timingType == DmaTimingType.Immediately)
            {
                channel.Enabled = false;
                channel.Control = (ushort)BitUtils.SetBit(channel.Control, 15, false);
            }
            else if (timingType is DmaTimingType.VBlank or DmaTimingType.Hblank)
            {
                channel.LengthInternal = channel.Count;
            }

            var index = Array.IndexOf(Channels, channel);
            if (index == 3 && channel.LengthInternal == 0)
            {
                channel.LengthInternal = 0x10000;
            }
            var destIncType = (channel.Control >> 5) & 0b11;
            var sourceIncType = (channel.Control >> 7) & 0b11;
            var repeat = ((channel.Control >> 9) & 1) == 1;
            var is32BitCopy = ((channel.Control >> 10) & 1) == 1;

            var originalLength = channel.LengthInternal;
            var unitSize = is32BitCopy ? 4u : 2u;

            //Console.WriteLine($"Writing Dma {index}, type: {timingType}");
            //Console.WriteLine($"control: 0x{channel.Control:x4}, src: 0x{channel.SourceInternal:x8}, dest: 0x{channel.DestinationInternal:x8}, unitSize: {unitSize}, count: {channel.LengthInternal}");

            for (; channel.LengthInternal > 0; channel.LengthInternal--)
            {
                if (is32BitCopy)
                {
                    var value = bus.Read32(channel.SourceInternal);
                    bus.Write32(channel.DestinationInternal, value);
                }
                else
                {
                    var value = bus.Read16(channel.SourceInternal);
                    bus.Write16(channel.DestinationInternal, value);
                }

                switch (sourceIncType)
                {
                    case 0:
                        channel.SourceInternal += unitSize;
                        break;
                    case 1:
                        channel.SourceInternal -= unitSize;
                        break;
                }
                switch (destIncType)
                {
                    case 0:
                        channel.DestinationInternal += unitSize;
                        break;
                    case 1:
                        channel.DestinationInternal -= unitSize;
                        break;
                    case 3:
                        channel.DestinationInternal += unitSize;
                        break;
                }
            }

            if (destIncType == 3)
            {
                channel.LengthInternal = originalLength;
                if (repeat)
                {
                    channel.DestinationInternal = channel.DestinationAddress;
                }
            }

            if ((channel.Control >> 14 & 1) != 1)
            {
                continue;
            }

            _interrupts.Request((InterruptType)((ushort)InterruptType.Dma0 << index));
        }
    }

    public void RunDmasT(DmaTimingType timingType, GbaBus bus)
    {
        foreach (var channel in Channels)
        {
            if ((channel.Control & 0x8000) != 0x8000 ||
                (DmaTimingType)((channel.Control >> 12) & 0b11) != timingType)
            {
                continue;
            }

            var index = Array.IndexOf(Channels, channel);
            var destIncType = (channel.Control >> 5) & 0b11;
            var sourceIncType = (channel.Control >> 7) & 0b11;
            var repeat = ((channel.Control >> 9) & 1) == 1;
            var is32BitCopy = ((channel.Control >> 10) & 1) == 1;

            var dest = channel.DestinationAddress;
            var source = channel.SourceAddress;
            var unitSize = is32BitCopy ? 4u : 2u;
            //Console.WriteLine($"Writing Dma {index}, type: {timingType}");
            //Console.WriteLine($"control: 0x{channel.Control:x4}, src: 0x{source:x8}, dest: 0x{dest:x8}, unitSize: {unitSize}, count: {channel.Count}");

            for (int i = 0; i < channel.Count; i++)
            {
                if (is32BitCopy)
                {
                    bus.Write32(dest, bus.Read32(source));
                }
                else
                {
                    bus.Write16(dest, bus.Read16(source));
                }

                switch (sourceIncType)
                {
                    case 0:
                        source += unitSize;
                        break;
                    case 1:
                        source -= unitSize;
                        break;
                }
                switch (destIncType)
                {
                    case 0:
                        dest += unitSize;
                        break;
                    case 1:
                        dest -= unitSize;
                        break;
                    case 3:
                        dest += unitSize;
                        break;
                }
            }

            channel.SourceAddress = source;
            channel.DestinationAddress = dest;
            if (!repeat || timingType == DmaTimingType.Immediately)
            {
                channel.Control &= 0x7fff;
            }
            else
            {
                //counter reload
                if (destIncType == 3) //and not fifo
                {
                    //reload dest
                }
            }

            if ((channel.Control >> 14 & 1) != 1)
            {
                continue;
            }

            var channelIndex = Array.IndexOf(Channels, channel);
            _interrupts.Request((InterruptType)((ushort)InterruptType.Dma0 << channelIndex));
        }
    }
}