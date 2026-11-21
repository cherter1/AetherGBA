using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AetherGba.Core.Video;

public sealed class FrameBuffer
{
    public const int Width = 240;// { get; }
    public const int Height = 160; // { get; }
    //abgr1555 color format
    /*
    public Span<ushort> Pixels
    {
        get
        {
            unsafe
            {
                //return _pixels;
                return new Span<ushort>(_pix, 240 * 160);
            }
        }
    }
    */

    //private readonly ushort[] _pixels;

    public readonly unsafe ushort* _pix;

    public unsafe FrameBuffer(int width, int height)
    {
        //Width = width;
        //Height = height;
        //_pixels = new ushort[width * height];
        _pix = (ushort*)Marshal.AllocHGlobal(Width * Height * sizeof(ushort)).ToPointer();
    }

    ~FrameBuffer()
    {
        unsafe
        {
            Marshal.FreeHGlobal((nint)_pix);
        }
    }

    public unsafe void SetPixel(int x, int y, ushort bgr)
    {
        /*
        if ((uint)x >= Width || (uint)y >= Height)
        {
            return;
        }
        */

        //alpha always set hi make color format abgr1555
        //_pixels[y * Width + x] = (ushort)(bgr | 0x8000);
        _pix[y * Width + x] = (ushort)(bgr | 0x8000);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void FillScanline(int scanline, ushort value)
    {
        ushort fill = (ushort)(value | 0x8000);
        ushort* p = _pix + (scanline * Width);
        for (int i = 0; i < Width; i++)
            p[i] = fill;
        //Pixels.Slice(scanline * Width, Width).Fill((ushort)(value | 0x8000));
    }
}