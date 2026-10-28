using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AetherGba.App.Wpf.Hosting;
using AetherGba.Core;
using AetherGba.Core.Input;

namespace AetherGba.App.Wpf;

public partial class MainWindow
{
    private readonly GbaMachine _machine;
    private readonly WriteableBitmap _bitmap;
    private readonly Int32Rect _frameRect;
    private readonly int _stride;

    private readonly Lock _lock = new();

    private byte[] _frontPixels;
    private byte[] _backPixels;
    private readonly byte[] _presentationPixels;
    private bool _frameReady;

    private readonly CancellationTokenSource _shutdown = new();
    private readonly Thread _emulationThread;

    private const double FramesPerSecond = 59.727500569606d;
    private static readonly double StopwatchTicksPerFrame = Stopwatch.Frequency / FramesPerSecond;

    public MainWindow(EmulatorStartup startup)
    {
        InitializeComponent();

        _machine = startup.Machine;

        var width = _machine.FrameBuffer.Width;
        var height = _machine.FrameBuffer.Height;

        _stride = width * 4;
        _frameRect = new Int32Rect(0, 0, width, height);

        _frontPixels = new byte[height * _stride];
        _backPixels = new byte[height * _stride];
        _presentationPixels = new byte[height * _stride];

        _bitmap = new WriteableBitmap(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null);

        FrameImage.Source = _bitmap;

        Title = startup.WindowTitle;
        StatusText.Text = startup.StatusMessage;

        CompositionTarget.Rendering += OnWpfRendering;

        _emulationThread = new Thread(EmulationLoop)
        {
            Name = "GBA Emulation",
            IsBackground = true
        };
        _emulationThread.Start();
        Closed += OnWindowClosed;
    }

    private void EmulationLoop()
    {
        CancellationToken token = _shutdown.Token;

        long loopStartTicks = Stopwatch.GetTimestamp();
        long completedFrames = 0;

        while (!token.IsCancellationRequested)
        {
            _machine.RunFrame();

            int count = _machine.FrameBuffer.Width * _machine.FrameBuffer.Height;
            for (int i = 0; i < count; i++)
            {
                ushort p = _machine.FrameBuffer.Pixels[i];
                _backPixels[i * 4] = Expand5To8[(p >> 10) & 0x1f]; //b
                _backPixels[i * 4 + 1] = Expand5To8[(p >> 5) & 0x1f]; //g
                _backPixels[i * 4 + 2] = Expand5To8[p & 0x1f]; //r
                _backPixels[i * 4 + 3] = 0xff; //a
            }

            lock (_lock)
            {
                (_frontPixels, _backPixels) = (_backPixels, _frontPixels);

                _frameReady = true;
            }

            completedFrames++;

            long nextFrameStartTicks = loopStartTicks + (long)Math.Round(completedFrames * StopwatchTicksPerFrame);

            long now = Stopwatch.GetTimestamp();

            //if we are more than 5 frames late
            if (now - nextFrameStartTicks > StopwatchTicksPerFrame * 5)
            {
                //restart fresh instead of trying to render all the stacked frames
                loopStartTicks = now;
                completedFrames = 0;
                continue;
            }

            WaitUntil(nextFrameStartTicks, token);
        }
    }

    private static void WaitUntil(long deadline, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            long remainingTicks = deadline - Stopwatch.GetTimestamp();

            if (remainingTicks <= 0)
            {
                return;
            }

            double remainingMilliseconds = remainingTicks * 1000.0 / Stopwatch.Frequency;

            if (remainingMilliseconds > 2.0)
            {
                int sleepMilliseconds = Math.Max(1, (int)remainingMilliseconds - 1);

                if (token.WaitHandle.WaitOne(sleepMilliseconds))
                {
                    return;
                }
            }
            else
            {
                Thread.SpinWait(32);
            }
        }
    }

    private void OnWpfRendering(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            if (!_frameReady)
            {
                return;
            }

            Buffer.BlockCopy(_frontPixels, 0, _presentationPixels, 0, _presentationPixels.Length);

            _frameReady = false;
        }

        _bitmap.WritePixels(_frameRect, _presentationPixels, _stride, 0);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnWpfRendering;

        _shutdown.Cancel();
        _emulationThread.Join(1000);

        _shutdown.Dispose();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!TryMapKey(e.Key, out var button)) return;

        _machine.Keypad.SetPressed(button, true);
        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (!TryMapKey(e.Key, out var button)) return;

        _machine.Keypad.SetPressed(button, false);
        e.Handled = true;
    }

    private static bool TryMapKey(Key key, out GbaButton button)
    {
        switch (key)
        {
            case Key.X:
                button = GbaButton.A;
                return true;
            case Key.Z:
                button = GbaButton.B;
                return true;
            case Key.A:
                button = GbaButton.L;
                return true;
            case Key.S:
                button = GbaButton.R;
                return true;
            case Key.Enter:
                button = GbaButton.Start;
                return true;
            case Key.RightShift:
            case Key.LeftShift:
                button = GbaButton.Select;
                return true;
            case Key.Up:
                button = GbaButton.Up;
                return true;
            case Key.Down:
                button = GbaButton.Down;
                return true;
            case Key.Left:
                button = GbaButton.Left;
                return true;
            case Key.Right:
                button = GbaButton.Right;
                return true;
            default:
                button = default;
                return false;
        }
    }

    //Array to expand a 5bit color to 8bit
    private static readonly byte[] Expand5To8 = BuildColorBitExpandTable();
    private static byte[] BuildColorBitExpandTable()
    {
        var table = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            table[i] = (byte)(i * 255 / 31);
        }

        return table;
    }
}