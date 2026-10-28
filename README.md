# Aether GBA

<div align="center">
<img width="256px" src="img/GbaEmu-Icon-256.png">
</div>

Gba Emulator work in progress

My hope with this project is to be able to reliably run most first party GBA games without any breaking issues and to make it cross platform.
This is my first attempt at an emulator of any kind so the code is pretty rough and the optimization needs a lot of work.
With SDL3 frontend it currently only runs on windows-x64 systems.

## Current State

### Projects
- `src/GbaEmulator.Core`: emulator core, CPU/bus/video/input/timers/DMA/interrupts
- `src/GbaEmulator.App.Wpf`: WPF desktop frontend
- `src/GbaEmulator.App.SDL`: SDL3 desktop frontend
- `tests/GbaEmulator.Core.Tests`: unit tests, with decent instruction coverage, but ive mostly been using test roms
- `tests/GbaEmulator.Core.Benchmarks`: very weak benchmarking of certain instructions types not fleshed out

### Cpu 
- Passing all ARM and THUMB tests from [jsmolka's gba-tests](https://github.com/jsmolka/gba-tests).
- The code is pretty rough and needs to cleanup and optimization
- Cycles are not fully accurate

### Memory
- No Open bus behavior implemented
- Cycle time and resolving the region is slow
- IO is very gross and needs a full refactor
- Saving is not fully implented yet

### Ppu
- Mostly implemented and working correct
- Blending works properly
- Affine sprites and backgrounds all work
- Windows are fully implemented but very slow and gross for resolving sprite window regions
- Cycles for sprites are not implemented properly so it will display all enabled, on screen sprites.
- No Mosaic

### Not Implented
- Audio is not implemented and I currently have no plans to do so
- Saving is not implemented fully yet
- SIO is not implemented and games with batteries will always be run dry
- Other event cycles such as from DMA are not accounted for at all
- I have no working BIOS functions so a working bios is required if you are relying on bios function behavior
- PPU Mosaic behavior is not implemented
- Im hoping to get some sort of debugger or visualizer made at some point to see instructions run and see vram frame info and IO registers

## Usage

### Prerequisites
- .NET 10 SDK Installed
- Must be on Windows (***Out of the box, see below***)
- If you are using the SDL frontend must be windows-x64 compatible (***Out of the box, see below***)
- If your ROM relies on BIOS functions (nearly all roms) you must get a GBA BIOS

### Running the application
Make sure you are in the repo root for all commands
### Tests

For the unit tests, you can simply run 
```powershell
dotnet test
```
for benchmarks
```powershell
dotnet run -c Release --project tests/GbaEmulator.Core.Benchmarks
```

### Emulator

Place *.gba ROM files in `roms/` at the repository root
- By default in `roms/` you will find `suite.gba` the file was compiled from [mGba's suite repo](https://github.com/mgba-emu/suite)

Place BIOS file in `bios/` at the repo root, and make sure the file is named `gba_bios.bin`

To run the SDL3 frontend that uses GPU rendering (<span style="font-size: 1.1em">***Recommended***</span>)
```powershell
dotnet run -c Release --project src/GbaEmulator.App.SDL
```

To run the WPF frontend (<span style="font-size: 1.1em">***Not Recommended***</span>)
```powershell
dotnet run -c Release --project src/GbaEmulator.App.Wpf
```

### Controls

#### Gba button mappings

| Gba button | Keyboard Key |
|------------|--------------|
| A          | X            |
| B          | Z            |
| L          | A            |
| R          | S            |
| Start      | Enter        |
| Select     | Shift        |
| Up         | Up Arrow     |
| Down       | Down Arrow   |
| Left       | Left Arrow   |
| Right      | Right Arrow  |

#### Other Emulator Functions

TODO: adding these actions (speed up can be done by commenting out the SDL delay function in program.cs)

| Action              | Keyboard Key |
|---------------------|--------------|
| Toggle Sped up mode | None         |
| Toggle fullscreen   | None         |

## Running on other platforms

Out of the box this emulator is only windows compatible \
but with some small changes can be used on other platforms with SDL

- In `src/GbaEmulator.App.SDL/runtimes` add two subfolders at this path `<platformArchitecture>/native`
- Download the SDL3 dll for your desired platform from the github [latest release](https://github.com/libsdl-org/SDL/releases/latest)
  - Optionally, to avoid potential issues you can match your desired platform version with the version of the dll for win-x64 that comes in the repo which is [version 3.4.16](https://github.com/libsdl-org/SDL/releases/tag/release-3.4.16)
- Next in the project file at `src/GbaEmulator.App.SDL/GbaEmulator.App.SDL.csproj` modify the include path of the dll to be correct
  - Currently looks like this
    ```xml
    <None Include="runtimes\win-x64\native\SDL3.dll"
          Link="SDL3.dll"
          CopyToOutputDirectory="PreserveNewest" />
    ```
  - Simply exchange the architecture with the one you used in the folder you made earlier and leave the rest as is
    ```xml
    <None Include="runtimes\<platformArchitecture>\native\SDL3.dll"
          Link="SDL3.dll"
          CopyToOutputDirectory="PreserveNewest" />
    ```
- Then you should be able to run `dotnet run --project src/GbaEmulator.App.SDL` without issue as long as youve followed the steps above and added your BIOS and ROM files to the proper spots 

## Game Screenshots
