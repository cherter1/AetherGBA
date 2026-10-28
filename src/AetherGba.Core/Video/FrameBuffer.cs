using System.Runtime.CompilerServices;

namespace AetherGba.Core.Video;

public sealed class FrameBuffer
{
    public int Width { get; }
    public int Height { get; }
    //abgr1555 color format
    public Span<ushort> Pixels => _pixels;
    private readonly ushort[] _pixels;

    public FrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new ushort[width * height];
    }

    public void SetPixel(int x, int y, ushort bgr)
    {
        if ((uint)x >= Width || (uint)y >= Height)
        {
            return;
        }

        //alpha always set hi make color format abgr1555
        _pixels[y * Width + x] = (ushort)(bgr | 0x8000);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillScanline(int scanline, ushort value)
    {
        Pixels.Slice(scanline * Width, Width).Fill((ushort)(value | 0x8000));
    }
}