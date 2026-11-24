using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AetherGba.Core.Video;

public sealed class FrameBuffer
{
    private const int Width = 240;
    private const int Height = 160;
    //abgr1555 color format
    public readonly unsafe ushort* Pix = (ushort*)NativeMemory.AllocZeroed(Width * Height * sizeof(ushort));

    public unsafe void SetPixel(int x, int y, ushort bgr)
    {
        //alpha always set hi make color format abgr1555
        Pix[y * Width + x] = (ushort)(bgr | 0x8000);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void FillScanline(int scanline, ushort value)
    {
        ushort fill = (ushort)(value | 0x8000);
        ushort* p = Pix + (scanline * Width);
        for (int i = 0; i < Width; i++)
            p[i] = fill;
    }
}