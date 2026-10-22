using GbaEmulator.App.SDL.Hosting;
using GbaEmulator.App.SDL.Native.Structs;
using GbaEmulator.Core.Input;
using SDL = GbaEmulator.App.SDL.Native.Sdl;
using static GbaEmulator.App.SDL.Native.Enums.Constants;

const ulong NativeFrameTicks = 16_744_118; //NS
var machine = GbaInit.CreateMachine();

SDL.SDL_Init(SDL_INIT_VIDEO);

var window = SDL.SDL_CreateWindow("Pixel Presentation Test", 240 * 2, 160 * 2, SDL_WINDOW_RESIZABLE | SDL_WINDOW_KEYBOARD_GRABBED);
var renderer = SDL.SDL_CreateRenderer(window, null);
var texture = SDL.SDL_CreateTexture(renderer, SDL_PIXELFORMAT_ABGR1555, SDL_TEXTUREACCESS_STREAMING, 240, 160); //streaming access

SDL.SDL_SetTextureScaleMode(texture, SDL_SCALEMODE_PIXELART); //pixel art mode

SDL.SDL_SetRenderLogicalPresentation(renderer, 240, 160, SDL_LOGICAL_PRESENTATION_LETTERBOX); //letterbox mode
SDL.SDL_SetRenderDrawColor(renderer, 0, 128, 255, 255);
SDL.SDL_RenderClear(renderer);
SDL.SDL_RenderPresent(renderer);

bool leftCtrlDown = false;
bool rightCtrlDown = false;
bool fKeyDown = false;

bool running = true;
ulong lastTime = 0, lastTicksNs = 0;
uint fpsCount = 0;
while (running)
{
    unsafe
    {
        SdlEvent sdlEvent;
        while (SDL.SDL_PollEvent(&sdlEvent))
        {
            if (sdlEvent.Type == SDL_EVENT_QUIT)
            {
                running = false;
                break;
            }

            if (sdlEvent.Type == SDL_EVENT_KEY_DOWN)
            {
                var keySetDown = *(SdlKeyboardEvent*)&sdlEvent;

                //mess with this approach to make toggle not held and not gross code
                if (keySetDown.KeyCode == SDLK_LCTRL)
                {
                    leftCtrlDown = true;
                }
                else if (keySetDown.KeyCode == SDLK_RCTRL)
                {
                    rightCtrlDown = true;
                }
                else if (keySetDown.KeyCode == SDLK_F)
                {
                    fKeyDown = true;
                }

                if (TryMapKey(keySetDown.KeyCode, out GbaButton button))
                {
                    Console.WriteLine($"key down: {button}");
                    machine.Keypad.SetPressed(button, true);
                }
            }
            else if (sdlEvent.Type == SDL_EVENT_KEY_UP)
            {
                var keySetUp = *(SdlKeyboardEvent*)&sdlEvent;

                //mess with this approach to make toggle not held and not gross code
                if (keySetUp.KeyCode == SDLK_LCTRL)
                {
                    leftCtrlDown = false;
                }
                else if (keySetUp.KeyCode == SDLK_RCTRL)
                {
                    rightCtrlDown = false;
                }
                else if (keySetUp.KeyCode == SDLK_F)
                {
                    fKeyDown = false;
                }

                if (TryMapKey(keySetUp.KeyCode, out GbaButton button))
                {
                    Console.WriteLine($"key up: {button}");
                    machine.Keypad.SetPressed(button, false);
                }
            }
        }

        machine.RunFrame();

        fixed (ushort* pixels = machine.FrameBuffer.Pixels)
        {
            SDL.SDL_UpdateTexture(texture, null, pixels, 240 * 2);
        }

        SDL.SDL_RenderClear(renderer);
        SDL.SDL_RenderTexture(renderer, texture, null, null);
        SDL.SDL_RenderPresent(renderer);
    }

    fpsCount++;

    ulong currentTime = SDL.SDL_GetTicks();
    if (currentTime > lastTime + 1000)
    {
        SDL.SDL_SetWindowTitle(window, $"FPS Test ({fpsCount} fps)");
        fpsCount = 0;
        lastTime = currentTime;
    }

    ulong currentTicksNs = SDL.SDL_GetTicksNS();
    ulong nextIntervalTicksNs = lastTicksNs + NativeFrameTicks;
    bool speedUpMode = fKeyDown && (rightCtrlDown || leftCtrlDown);
    if (currentTicksNs < nextIntervalTicksNs && !speedUpMode)
    {
        SDL.SDL_DelayNS(nextIntervalTicksNs - currentTicksNs);
    }
    lastTicksNs = SDL.SDL_GetTicksNS();
}

SDL.SDL_DestroyTexture(texture);
SDL.SDL_DestroyRenderer(renderer);
SDL.SDL_DestroyWindow(window);
SDL.SDL_Quit();
return 0;

static bool TryMapKey(uint k, out GbaButton button)
{
    switch (k)
    {
        case SDLK_X:
            button = GbaButton.A;
            return true;
        case SDLK_Z:
            button = GbaButton.B;
            return true;
        case SDLK_A:
            button = GbaButton.L;
            return true;
        case SDLK_S:
            button = GbaButton.R;
            return true;
        case SDLK_RETURN:
        case SDLK_KP_ENTER:
            button = GbaButton.Start;
            return true;
        case SDLK_LSHIFT:
        case SDLK_RSHIFT:
            button = GbaButton.Select;
            return true;
        case SDLK_UP:
            button = GbaButton.Up;
            return true;
        case SDLK_DOWN:
            button = GbaButton.Down;
            return true;
        case SDLK_LEFT:
            button = GbaButton.Left;
            return true;
        case SDLK_RIGHT:
            button = GbaButton.Right;
            return true;
        default:
            button = default;
            return false;
    }
}