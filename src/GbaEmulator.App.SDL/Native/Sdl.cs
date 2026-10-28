using System.Runtime.InteropServices;
using GbaEmulator.App.SDL.Native.Structs;

namespace GbaEmulator.App.SDL.Native;

internal static unsafe partial class Sdl
{
    private const string LibraryName = "SDL3";

    #region SDL_init.h

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_Init(uint flags);

    [LibraryImport(LibraryName)]
    internal static partial void SDL_Quit();

    #endregion

    #region SDL_error.h

    [LibraryImport(LibraryName)]
    internal static partial nint SDL_GetError();

    #endregion

    #region SDL_video.h

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SDL_CreateWindow(string title, int width, int height, ulong flags); //SDL_WindowFlags

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetWindowTitle(nint window, string title);

    [LibraryImport(LibraryName)]
    internal static partial void SDL_DestroyWindow(nint window);

    #endregion

    #region SDL_render.h

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SDL_CreateRenderer(nint window, string? name);

    [LibraryImport(LibraryName)]
    internal static partial void SDL_DestroyRenderer(nint renderer);

    [LibraryImport(LibraryName)]
    internal static partial nint SDL_CreateTexture(nint renderer, uint format, int access, int w, int h);

    [LibraryImport(LibraryName)]
    internal static partial void SDL_DestroyTexture(nint texture);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetTextureScaleMode(nint texture, int scaleMode);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_UpdateTexture(nint texture, void* rect, void* pixels, int pitch);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RenderTexture(nint renderer, nint texture, void* srcrect, void* dstrect);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetRenderLogicalPresentation(nint renderer, int w, int h, int mode);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_SetRenderDrawColor(nint renderer, byte r, byte g, byte b, byte a);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RenderPresent(nint renderer);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_RenderClear(nint renderer);

    #endregion

    #region SDL_timer.h

    [LibraryImport(LibraryName)]
    internal static partial void SDL_DelayNS(ulong ns);

    [LibraryImport(LibraryName)]
    internal static partial ulong SDL_GetTicks(); //ms elapsed

    [LibraryImport(LibraryName)]
    internal static partial ulong SDL_GetTicksNS(); //ns elapsed

    #endregion

    #region SDL_events.h

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool SDL_PollEvent(SdlEvent* sdlEvent);

    #endregion
}