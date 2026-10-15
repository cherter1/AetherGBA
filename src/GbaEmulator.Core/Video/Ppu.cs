using System.Runtime.CompilerServices;
using GbaEmulator.Core.Common;
using GbaEmulator.Core.Dma;
using GbaEmulator.Core.Interrupts;
using GbaEmulator.Core.Memory;
using GbaEmulator.Core.Video.Backgrounds;
using GbaEmulator.Core.Video.SpecialEffects;
using GbaEmulator.Core.Video.Sprites;

namespace GbaEmulator.Core.Video;

public sealed class Ppu
{
    private const int ScreenWidth = 240;
    private const int ScreenHeight = 160;
    private const int CyclesPerScanline = 1232;
    private const int ScanLinesPerFrame = 228;
    private const int HBlankStartCycle = 1006;

    private readonly InterruptController _interrupts;
    private readonly DmaController _dma;
    private readonly GbaMemory _memory;

    public Ppu(InterruptController interrupts, DmaController dma, GbaMemory memory)
    {
        _interrupts = interrupts;
        _memory = memory;
        _dma = dma;
        FrameBuffer = new FrameBuffer(ScreenWidth, ScreenHeight);
    }

    public FrameBuffer FrameBuffer { get; }

    private int _scanlineCycle;

    public void Step(int cycles, GbaBus bus)
    {
        while (cycles > 0)
        {
            //if is in HBlank then cycle boundary is start of next scanline otherwise its in the start of HBlank
            int nextBoundary = _scanlineCycle >= HBlankStartCycle ? CyclesPerScanline : HBlankStartCycle;
            int cyclesUntilBoundary = nextBoundary - _scanlineCycle;
            int consumed = Math.Min(cycles, cyclesUntilBoundary);

            _scanlineCycle += consumed;
            cycles -= consumed;

            if (_scanlineCycle != nextBoundary)
            {
                continue; //continue til hblank start
            }

            if (nextBoundary == HBlankStartCycle)
            {
                EnterHBlank(bus); // enter hblank, trigger dma, interrupt, and renderScanLine
            }
            else
            {
                EndScanline(bus); // leave hblank, update vcount, enter vblank when appropriate
            }
        }
    }

    private void EnterHBlank(GbaBus bus)
    {
        _memory.Io.REG_DISPSTAT = (ushort)BitUtils.SetBit(_memory.Io.REG_DISPSTAT, 1, true); //set hblank

        if (_memory.Io.REG_VCOUNT < ScreenHeight)
        {
            _dma.RunDmas(DmaTimingType.Hblank, bus);
        }

        if (BitUtils.IsBitSet(_memory.Io.REG_VCOUNT, 4)) // hlbank irq enabled
        {
            _interrupts.Request(InterruptType.HBlank);
        }

        if (_memory.Io.REG_VCOUNT < ScreenHeight) //Only Render Visible scan lines
        {
            RenderScanLine(_memory.Io.REG_VCOUNT);
        }
    }

    public bool FrameReady { get; set; }
    private void EndScanline(GbaBus bus)
    {
        _memory.Io.REG_DISPSTAT = (ushort)BitUtils.SetBit(_memory.Io.REG_DISPSTAT, 1, false); // leave hblank unset bit

        _scanlineCycle = 0;

        int nextLine = _memory.Io.REG_VCOUNT + 1;

        if (nextLine == ScanLinesPerFrame)
        {
            nextLine = 0;
            FrameReady = true;
        }

        _memory.Io.REG_VCOUNT = (ushort)nextLine;

        UpdateVBlankState(bus); // on scanline 160 enter blank, and do dma and interrupt, on zero leave blank
        UpdateVCountMatch(); // check if vcount triggered and do interrupt
    }

    private void UpdateVBlankState(GbaBus bus)
    {
        ushort vcount = _memory.Io.REG_VCOUNT;

        switch (vcount)
        {
            case ScreenHeight:
                {
                    _memory.Io.REG_DISPSTAT = (ushort)BitUtils.SetBit(_memory.Io.REG_DISPSTAT, 0, true);

                    _dma.RunDmas(DmaTimingType.VBlank, bus);

                    if (BitUtils.IsBitSet(_memory.Io.REG_DISPSTAT, 3)) //vblank irq enabled
                    {
                        _interrupts.Request(InterruptType.VBlank);
                    }

                    break;
                }
            case 0:
                _memory.Io.REG_DISPSTAT = (ushort)BitUtils.SetBit(_memory.Io.REG_DISPSTAT, 0, false); // leave vblank
                break;
        }
    }

    private void UpdateVCountMatch()
    {
        ushort dispStat = _memory.Io.REG_DISPSTAT;
        int compareValue = (dispStat >> 8) & 0xFF;
        bool wasMatching = BitUtils.IsBitSet(dispStat, 2); //vcount triggered status
        bool isMatching = _memory.Io.REG_VCOUNT == compareValue; //trigger if line trigger from DISPSTAT equals vcount reg
        _memory.Io.REG_DISPSTAT = (ushort)BitUtils.SetBit(_memory.Io.REG_DISPSTAT, 2, isMatching); //set based on if line trigger matching

        if (!wasMatching &&
            isMatching &&
            BitUtils.IsBitSet(_memory.Io.REG_DISPSTAT, 5)) //trigger interrupt if vcount enabled and if vcount == trigger value and was not already set
        {
            _interrupts.Request(InterruptType.VCounter);
        }
    }

    private void RenderScanLine(int scanLine)
    {
        var modeBits = _memory.Io.REG_DISPCNT & 0x7; //bits 0-2
        if (BitUtils.IsBitSet(_memory.Io.REG_DISPCNT, 7)) //if bit 7 is set force blank
        {
            modeBits = 0xff;
        }

        switch (modeBits)
        {
            case 0: //tile/map based text mode
                RenderMode0(scanLine);
                break;
            case 1: //tile/map based text mode with affine for bg2
                RenderMode1(scanLine);
                break;
            case 2: //tile/map based rotation/scaling mode
                RenderMode2(scanLine);
                break;
            case 3: //bitmap based mode for still images
                RenderMode3(scanLine);
                break;
            case 4: //bitmap based mode
                RenderMode4(scanLine);
                break;
            case 5: //bitmap based mode
                RenderMode5(scanLine);
                break;
            default:
                //should only be used when forceBlank bit is set in DisplayControl register
                var backDropColor = (ushort)(_memory.PaletteRam[0] | (_memory.PaletteRam[1] << 8)); //backdrop color is set by zero index of palette
                FrameBuffer.FillScanline(scanLine, backDropColor);
                break;
        }
    }

    private void RenderMode0(int y)
    {
        var displayControl = _memory.Io.REG_DISPCNT;
        var inWin0YRange = SpecialEffectsHelper.TryGetWindowRange(windowNum: 0, y, displayControl,
            _memory.Io.REG_WIN0H,
            _memory.Io.REG_WIN0V,
            out byte win0StartX,
            out uint win0XThreshold);
        var inWin1YRange = SpecialEffectsHelper.TryGetWindowRange(windowNum: 1, y, displayControl,
            _memory.Io.REG_WIN1H,
            _memory.Io.REG_WIN1V,
            out byte win1StartX,
            out uint win1XThreshold);

        var objWinEnabled = (displayControl & 0x8000) != 0; //bit 15 for objWindow enabled
        if (objWinEnabled) _objectWindowMask.AsSpan().Clear(); //clear previous scanLines objWin mask

        ReadOnlySpan<ushort> bgControls =
        [
            _memory.Io.REG_BG0CNT, _memory.Io.REG_BG1CNT, _memory.Io.REG_BG2CNT, _memory.Io.REG_BG3CNT
        ];
        ReadOnlySpan<ushort> hofsTable =
        [
            _memory.Io.REG_BG0HOFS, _memory.Io.REG_BG1HOFS,
            _memory.Io.REG_BG2HOFS, _memory.Io.REG_BG3HOFS
        ];
        Span<int> bgOutputBuffer = stackalloc int[4];
        int enabledBgCount = FastSortBackgroundsByPriority(displayControl, bgControls, bgOutputBuffer, 0b1111);
        Span<int> activeBgs = bgOutputBuffer[..enabledBgCount];
        ReadOnlySpan<TextBackgroundScanlineInfo> bgScanlineInfo =
        [
            new(y, _memory.Io.REG_BG0VOFS, bgControls[0]),
            new(y, _memory.Io.REG_BG1VOFS, bgControls[1]),
            new(y, _memory.Io.REG_BG2VOFS, bgControls[2]),
            new(y, _memory.Io.REG_BG3VOFS, bgControls[3])
        ];

        var spriteCount = 0;
        Span<ScanlineSpriteInfo> spriteBuffer = stackalloc ScanlineSpriteInfo[128];
        if ((displayControl & 0x1000) == 0x1000) //bit 12 set means sprites enabled
        {
            spriteCount = GetScanlineVisibleSprites(y, spriteBuffer);
        }

        Span<ScanlineSpriteInfo> visibleSprites = spriteBuffer[..spriteCount];
        SortSpriteIndicesByPriority(visibleSprites);

        ReadOnlySpan<byte> vram = _memory.Vram.AsSpan(0, 0x18000); //Explicit slice with full Length of VRAM to help JIT eliminate bounds checks
        ReadOnlySpan<byte> paletteRam = _memory.PaletteRam.AsSpan(0, 0x400);
        for (var x = 0; x < ScreenWidth; x++)
        {
            var winMask = 0b111111;
            if ((displayControl & 0xe000) != 0) //bits 15-13 are window enable bits
            {
                winMask = _memory.Io.REG_WINOUT & 0x3f;
                if (inWin0YRange && (uint)(x - win0StartX) < win0XThreshold) //implied from in y range that its enabled see above
                {
                    winMask = _memory.Io.REG_WININ & 0x3f;
                }
                else if (inWin1YRange && (uint)(x - win1StartX) < win1XThreshold) //implied from in y range that its enabled see above
                {
                    winMask = (_memory.Io.REG_WININ >> 8) & 0x3f;
                }
                else if (objWinEnabled && _objectWindowMask[x] != 0)
                {
                    winMask = (_memory.Io.REG_WINOUT >> 8) & 0x3f;
                }
            }

            ushort hiBgrColor, loBgrColor = hiBgrColor = 0x8000;
            BlendTargetOneType hiColorSource = BlendTargetOneType.Backdrop;
            BlendTargetTwoType loColorSource = BlendTargetTwoType.Backdrop;

            int hiPriorityLine, loPriorityLine = hiPriorityLine = 4; //priority line for hi and lo pixels default 4 to always get beat initially
            foreach (ref readonly var bgIdx in activeBgs)
            {
                if ((winMask & (1 << bgIdx)) == 0) //if not set to display in window continue
                {
                    continue;
                }

                var isSinglePalette = bgScanlineInfo[bgIdx].IsSinglePalette;
                var paletteIndex = RenderTiledTextBackground(in vram, x, hofsTable[bgIdx],
                    tileMapStartOffset: bgScanlineInfo[bgIdx].TileMapStartOffset,
                    xTiles: bgScanlineInfo[bgIdx].XTiles,
                    tileY: bgScanlineInfo[bgIdx].CurrentYTile,
                    pixelYInTile: bgScanlineInfo[bgIdx].PixelYInTile,
                    isSinglePalette,
                    tileDataStartOffset: bgScanlineInfo[bgIdx].TileDataStartOffset);

                if (paletteIndex == 0)
                {
                    continue;
                }
                if (!isSinglePalette && (paletteIndex & 0xf) == 0) //mod 16 cuz if zero index of any palette color is transparent in 4bpp mode
                {
                    continue;
                }

                var offset = paletteIndex << 1; // mul 2 because each palette entry is 2 bytes
                var bgrColor = (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

                if (hiBgrColor == 0x8000)
                {
                    hiBgrColor = bgrColor;
                    hiColorSource = (BlendTargetOneType)(1 << bgIdx);
                    hiPriorityLine = bgControls[bgIdx] & 0b11;
                    continue;
                }

                loBgrColor = bgrColor;
                loColorSource = (BlendTargetTwoType)(1 << (bgIdx + 8));
                loPriorityLine = bgControls[bgIdx] & 0b11;
                break;
            }

            int spriteMode = 0;
            foreach (ref readonly var sprite in visibleSprites)
            {
                if ((winMask & 0x10) == 0) //bit 4 of winMask is to display objects
                {
                    continue;
                }

                int objPaletteIndex = sprite.IsAffine
                    ? RenderAffineSprite(in vram, in sprite, x, loPriorityLine, displayControl)
                    : RenderRegularSprite(in vram, in sprite, x, loPriorityLine);

                if (objPaletteIndex == 0) continue;

                objPaletteIndex += (16 * (sprite.SinglePalette ? 0 : sprite.PaletteNumber));
                var offset = (objPaletteIndex << 1) + 0x200; //object palettes start offset 0x200 in paletteRam
                var objPixelColor = (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

                if (sprite.Priority <= hiPriorityLine) //if sprite has higher priority than current top pixel
                {
                    //make sprite pixel top and next top gets set to previous top
                    (loBgrColor, loColorSource) = (hiBgrColor, hiColorSource.ToBlendTargetTwoType());
                    (hiBgrColor, hiColorSource) = (objPixelColor, BlendTargetOneType.Obj);
                    spriteMode = sprite.Mode;
                    break;
                }

                loBgrColor = objPixelColor;
                loColorSource = BlendTargetTwoType.Obj;
                break;
            }

            if (loBgrColor == 0x8000)
            {
                loBgrColor = (ushort)(paletteRam[0] | (paletteRam[1] << 8));
                if (hiBgrColor == 0x8000)
                {
                    hiBgrColor = loBgrColor;
                }
            }

            if ((winMask & 0x20) != 0) //if window mask bit 5 not set then window's special effects disabled
            {
                if (hiColorSource == BlendTargetOneType.Obj && spriteMode == 1)
                {
                    //will always use alpha blending with this as source regardless of BLDCNT
                    var t2BlendingEnabled = (_memory.Io.REG_BLDCNT & (ushort)loColorSource) != 0;
                    if (t2BlendingEnabled)
                    {
                        hiBgrColor = SpecialEffectsHelper.AlphaBlendPixels(hiBgrColor, loBgrColor, _memory.Io.REG_BLDALPHA);
                    }
                    else if (((_memory.Io.REG_BLDCNT >> 6) & 0b11) != 0b00) //blend control bits 6-7 not zero then apply blending
                    {
                        hiBgrColor = ApplyBlendingEffects(hiBgrColor, hiColorSource, loBgrColor, loColorSource);
                    }
                }
                else if (((_memory.Io.REG_BLDCNT >> 6) & 0b11) != 0b00) //blend control bits 6-7 not zero then apply blending
                {
                    hiBgrColor = ApplyBlendingEffects(hiBgrColor, hiColorSource, loBgrColor, loColorSource);
                }
            }

            FrameBuffer.SetPixel(x, y, hiBgrColor);
        }
    }

    private static int RenderAffineSprite(in ReadOnlySpan<byte> vram, in ScanlineSpriteInfo sprite, int x, int priorityLine, ushort displayControl)
    {
        var spriteXPos = x - sprite.XCoord;

        var canvasWidth = sprite.DoubleSize ? (sprite.XTiles * 8) * 2 : sprite.XTiles * 8; //for now hFlip is DoubleSize flag for rotationalSprites
        if ((uint)spriteXPos >= (uint)canvasWidth || sprite.Priority > priorityLine)
        {
            return 0; //sprite not in x range or lower priority than nextTopPixel
        }

        int relativeX = spriteXPos - canvasWidth / 2; //center x pixel of sprite
        int relativeY = sprite.RelativeY; //for now YPixelOffset is relativeY for rotational sprites

        int sourceX = ((sprite.Pa * relativeX + sprite.Pb * relativeY) >> 8) + (sprite.XTiles * 8) / 2;
        int sourceY = ((sprite.Pc * relativeX + sprite.Pd * relativeY) >> 8) + (sprite.YTiles * 8) / 2;

        if ((uint)sourceX >= (uint)(sprite.XTiles * 8) || (uint)sourceY >= (uint)(sprite.YTiles * 8))
        {
            return 0; //transformed coordinate is outsize the sprite graphics
        }

        var sourceXTileNumber = sourceX >> 3; //div 8
        var sourceXPixelNumber = sourceX & 7; //mod 8
        var sourceYTileNumber = sourceY >> 3; //div 8
        var sourceYPixelNumber = sourceY & 7; //mod 8

        var yPixelOffset = sourceYPixelNumber * 4; //mul 4(4bppMode) for pixel inside tile offset
        var startTileNumber = sprite.TileNumber; //for now ScanlineStartMapTileNumber is attr2 tileNumber for rotationalSprites
        var twoDMatrixSize = 32;

        if (sprite.SinglePalette)
        {
            startTileNumber /= 2;
            yPixelOffset *= 2; //mul 2 (8 total bc already mul 4 above) in 8bpp mode because each byte is a pixel
            twoDMatrixSize = 16;
        }

        int scanlineStartMapTileNumber;
        if ((displayControl & 0x40) == 0x40) //bit 6 set then 1d char mapping
        {
            scanlineStartMapTileNumber = startTileNumber + (sourceYTileNumber * sprite.XTiles);
        }
        else //bit 6 clear 2d character mapping
        {
            scanlineStartMapTileNumber = startTileNumber + (sourceYTileNumber * twoDMatrixSize);
        }

        var sourceMapTileNumber = scanlineStartMapTileNumber + sourceXTileNumber;
        var sourceTileOffset = 0x10000 + (sourceMapTileNumber * (sprite.SinglePalette ? 0x40 : 0x20));
        var sourcePixOffset = sourceTileOffset + yPixelOffset + (sourceXPixelNumber >> (sprite.SinglePalette ? 0 : 1)); //divide x by 2 if 4bpp
        var sourceObjPaletteIndex = sprite.SinglePalette
            ? vram[sourcePixOffset]
            : (sourceXPixelNumber & 1) == 0
                ? vram[sourcePixOffset] & 0xf
                : vram[sourcePixOffset] >> 4;
        return sourceObjPaletteIndex;
    }

    private static int SampleAffineSpritePixel(in ReadOnlySpan<byte> vram, in ScanlineSpriteInfo sprite, int x, ushort displayControl, int canvasWidth)
    {
        int relativeX = x - canvasWidth / 2;
        int relativeY = sprite.RelativeY; //for now YPixelOffset is relativeY for rotational sprites

        int sourceX = ((sprite.Pa * relativeX + sprite.Pb * relativeY) >> 8) + (sprite.XTiles * 8) / 2;
        int sourceY = ((sprite.Pc * relativeX + sprite.Pd * relativeY) >> 8) + (sprite.YTiles * 8) / 2;

        if ((uint)sourceX >= (uint)(sprite.XTiles * 8) || (uint)sourceY >= (uint)(sprite.YTiles * 8))
        {
            return 0; //transformed coordinate is outsize the sprite graphics
        }

        var sourceXTileNumber = sourceX >> 3; //div 8
        var sourceXPixelNumber = sourceX & 7; //mod 8
        var sourceYTileNumber = sourceY >> 3; //div 8
        var sourceYPixelNumber = sourceY & 7; //mod 8

        var yPixelOffset = sourceYPixelNumber * 4; //mul 4(4bppMode) for pixel inside tile offset
        var startTileNumber = sprite.TileNumber; //for now ScanlineStartMapTileNumber is attr2 tileNumber for rotationalSprites
        var twoDMatrixSize = 32;

        if (sprite.SinglePalette)
        {
            startTileNumber /= 2;
            yPixelOffset *= 2; //mul 2 (8 total bc already mul 4 above) in 8bpp mode because each byte is a pixel
            twoDMatrixSize = 16;
        }

        int scanlineStartMapTileNumber;
        if ((displayControl & 0x40) == 0x40) //bit 6 set then 1d char mapping
        {
            scanlineStartMapTileNumber = startTileNumber + (sourceYTileNumber * sprite.XTiles);
        }
        else //bit 6 clear 2d character mapping
        {
            scanlineStartMapTileNumber = startTileNumber + (sourceYTileNumber * twoDMatrixSize);
        }

        var sourceMapTileNumber = scanlineStartMapTileNumber + sourceXTileNumber;
        var sourceTileOffset = 0x10000 + (sourceMapTileNumber * (sprite.SinglePalette ? 0x40 : 0x20));
        var sourcePixOffset = sourceTileOffset + yPixelOffset + (sourceXPixelNumber >> (sprite.SinglePalette ? 0 : 1)); //divide x by 2 if 4bpp
        var sourceObjPaletteIndex = sprite.SinglePalette
            ? vram[sourcePixOffset]
            : (sourceXPixelNumber & 1) == 0
                ? vram[sourcePixOffset] & 0xf
                : vram[sourcePixOffset] >> 4;
        return sourceObjPaletteIndex;
    }

    private static int RenderRegularSprite(in ReadOnlySpan<byte> vram, in ScanlineSpriteInfo sprite, int x, int priorityLine)
    {
        var spriteXPos = x - sprite.XCoord;

        bool objXRange = (uint)spriteXPos < (uint)(sprite.XTiles * 8);
        if (!objXRange || sprite.Priority > priorityLine)
        {
            return 0;
        }

        var currentXTileNumber = spriteXPos >> 3; //divide 8
        var currentXPixelNumber = spriteXPos & 7; // mod 8
        if (sprite.HFlip)
        {
            currentXTileNumber = (sprite.XTiles - 1) - currentXTileNumber;
            currentXPixelNumber = 7 - currentXPixelNumber;
        }
        var currentMapTileNumber = sprite.ScanlineStartMapTileNumber + currentXTileNumber;
        var currentTileOffset = 0x10000 + (currentMapTileNumber * (sprite.SinglePalette ? 0x40 : 0x20));
        var currentPixOffset = currentTileOffset + sprite.YPixelOffset + (currentXPixelNumber >> (sprite.SinglePalette ? 0 : 1)); //divide x by 2 if 4bpp
        var objPaletteIndex = sprite.SinglePalette
            ? vram[currentPixOffset]
            : (currentXPixelNumber & 1) == 0
                ? vram[currentPixOffset] & 0xf
                : vram[currentPixOffset] >> 4;

        return objPaletteIndex;
    }

    private static int SampleRegularSpritePixel(in ReadOnlySpan<byte> vram, in ScanlineSpriteInfo sprite, int x)
    {
        var spriteX = x - sprite.XCoord;

        var currentXTileNumber = spriteX >> 3; //divide 8
        var currentXPixelNumber = spriteX & 7; // mod 8
        if (sprite.HFlip)
        {
            currentXTileNumber = (sprite.XTiles - 1) - currentXTileNumber;
            currentXPixelNumber = 7 - currentXPixelNumber;
        }
        var currentMapTileNumber = sprite.ScanlineStartMapTileNumber + currentXTileNumber;
        var currentTileOffset = 0x10000 + (currentMapTileNumber * (sprite.SinglePalette ? 0x40 : 0x20));
        var currentPixOffset = currentTileOffset + sprite.YPixelOffset + (currentXPixelNumber >> (sprite.SinglePalette ? 0 : 1)); //divide x by 2 if 4bpp
        var objPaletteIndex = sprite.SinglePalette
            ? vram[currentPixOffset]
            : (currentXPixelNumber & 1) == 0
                ? vram[currentPixOffset] & 0xf
                : vram[currentPixOffset] >> 4;

        return objPaletteIndex;
    }

    private static int RenderTiledTextBackground(in ReadOnlySpan<byte> vram, int x, ushort hofs, int tileMapStartOffset,
        int xTiles, int tileY, int pixelYInTile, bool isSinglePalette, int tileDataStartOffset)
    {
        var bgXStartOffset = x + hofs;
        if (xTiles > 32 && ((bgXStartOffset >> 8) & 1) != 0) // startOffset greater than pixels per map or SE length across AND xTiles > 32, if its 32 mirror the single X SE
        {
            tileMapStartOffset += 0x800; //move startOffset to next SE(map) start offset
        }
        bgXStartOffset &= 0xff;

        var tileX = bgXStartOffset >> 3; // div 8 to count tiles from offset
        var pixelXInTile = bgXStartOffset & 7; // modulo 8 for pixel 0-7 on x axis

        var tileMapIndex = tileY * 32 + tileX; //tileY * 32 + tileX
        var tileMapEntryOffset = tileMapStartOffset + tileMapIndex * 2; //tileMapStartOffset + mapIndex * mapEntrySize
        var tileMapEntry = Read16(in vram, tileMapEntryOffset);

        var hFlip = (tileMapEntry & 0x0400) != 0;
        var vFlip = (tileMapEntry & 0x0800) != 0;
        if (hFlip) pixelXInTile = 7 - pixelXInTile;
        if (vFlip) pixelYInTile = 7 - pixelYInTile;

        int paletteIndex;
        if (isSinglePalette) //8bpp mode
        {
            var tileIndex = tileMapEntry & 0x03ff;
            var currentTileOffset = tileDataStartOffset + tileIndex * 0x40; //tileDataStartOffset + (tileIndex * sizeof Tile(bytes))
            var currentPixelOffset = currentTileOffset + (pixelYInTile * 8) + pixelXInTile;
            paletteIndex = vram[currentPixelOffset];
        }
        else //4bpp mode
        {
            var tileIndex = tileMapEntry & 0x03ff;
            var currentTileOffset = tileDataStartOffset + tileIndex * 0x20; //tileDataStartOffset + (tileIndex * sizeof Tile(bytes))
            var currentPixelOffset = currentTileOffset + (pixelYInTile * 4) + (pixelXInTile >> 1); //YPixel * 4 bc 2 pixels per byte in 8pixel row XPixel /2 for same reason
            paletteIndex = (pixelXInTile & 1) == 0 // mod by 2 if even Index take bits 0-3 else take bits 4-7
                ? vram[currentPixelOffset] & 0xf
                : vram[currentPixelOffset] >> 4;
            paletteIndex += (tileMapEntry >> 12) * 16; // add palette bank start offset
        }

        return paletteIndex;
    }

    private ushort ApplyBlendingEffects(ushort t1PixelBgr555Color, BlendTargetOneType t1ControlBit, ushort t2PixelBgr555Color, BlendTargetTwoType t2ControlBit)
    {
        var blendControl = _memory.Io.REG_BLDCNT;
        var blendMode = (blendControl >> 6) & 0b11; //bits 6-7 blend mode

        var t1BlendingEnabled = (blendControl & (ushort)t1ControlBit) == (ushort)t1ControlBit;
        if (!t1BlendingEnabled)
        {
            //top is not enabled as target1 don't blend
            return t1PixelBgr555Color;
        }

        ushort finalColor = 1;
        switch (blendMode)
        {
            case 0b01: //alpha blending
                var t2BlendingEnabled = (blendControl & (ushort)t2ControlBit) == (ushort)t2ControlBit;
                if (!t2BlendingEnabled)
                {
                    //nextTop not enabled as target2 don't blend
                    return t1PixelBgr555Color;
                }
                finalColor = SpecialEffectsHelper.AlphaBlendPixels(t1PixelBgr555Color, t2PixelBgr555Color, _memory.Io.REG_BLDALPHA);
                break;
            case 0b10: //brightness increase
                finalColor = SpecialEffectsHelper.LightenBlend(t1PixelBgr555Color, _memory.Io.REG_BLDY);
                break;
            case 0b11: //brightness decrease
                finalColor = SpecialEffectsHelper.DarkenBlend(t1PixelBgr555Color, _memory.Io.REG_BLDY);
                break;
        }

        return finalColor;
    }

    private void RenderMode1(int y)
    {
        if (y == 0)
        {
            _memory.Io.InternalBg2X = BitUtils.SignExtend((int)_memory.Io.REG_BG2X, 28);
            _memory.Io.InternalBg2Y = BitUtils.SignExtend((int)_memory.Io.REG_BG2Y, 28);
        }

        var displayControl = _memory.Io.REG_DISPCNT;
        var inWin0YRange = SpecialEffectsHelper.TryGetWindowRange(windowNum: 0, y, displayControl,
            _memory.Io.REG_WIN0H,
            _memory.Io.REG_WIN0V,
            out byte win0StartX,
            out uint win0XThreshold);
        var inWin1YRange = SpecialEffectsHelper.TryGetWindowRange(windowNum: 1, y, displayControl,
            _memory.Io.REG_WIN1H,
            _memory.Io.REG_WIN1V,
            out byte win1StartX,
            out uint win1XThreshold);

        var objWinEnabled = (displayControl & 0x8000) != 0; //bit 15 for objWindow enabled
        if (objWinEnabled) _objectWindowMask.AsSpan().Clear(); //clear previous scanLines objWin mask

        int fixedSourceX = _memory.Io.InternalBg2X, fixedSourceY = _memory.Io.InternalBg2Y;
        var bg2Size = BackgroundHelpers.GetRotationalBackgroundSizePixels((_memory.Io.REG_BG2CNT >> 14) & 0b11);
        ReadOnlySpan<ushort> bgControls = [_memory.Io.REG_BG0CNT, _memory.Io.REG_BG1CNT, _memory.Io.REG_BG2CNT];
        ReadOnlySpan<ushort> hofsTable = [_memory.Io.REG_BG0HOFS, _memory.Io.REG_BG1HOFS];
        Span<int> bgOutputBuffer = stackalloc int[3];
        var enabledBgCount = FastSortBackgroundsByPriority(displayControl, bgControls, bgOutputBuffer, 0b0111);
        Span<int> activeBgs = bgOutputBuffer[..enabledBgCount];
        ReadOnlySpan<TextBackgroundScanlineInfo> bgScanlineInfos =
        [
            new(y, _memory.Io.REG_BG0VOFS, bgControls[0]),
            new(y, _memory.Io.REG_BG1VOFS, bgControls[1])
        ];

        var spriteCount = 0;
        Span<ScanlineSpriteInfo> spriteBuffer = stackalloc ScanlineSpriteInfo[128];
        if ((displayControl & 0x1000) != 0) //bit 12 set means sprites enabled
        {
            spriteCount = GetScanlineVisibleSprites(y, spriteBuffer);
        }
        Span<ScanlineSpriteInfo> visibleSprites = spriteBuffer[..spriteCount];
        SortSpriteIndicesByPriority(visibleSprites);

        ReadOnlySpan<byte> vram = _memory.Vram.AsSpan(0, 0x18000); //Explicit slice with full Length of VRAM to help JIT eliminate bounds checks
        ReadOnlySpan<byte> paletteRam = _memory.PaletteRam.AsSpan(0, 0x400);
        for (int x = 0; x < ScreenWidth; x++)
        {
            var winMask = 0b111111;
            if ((displayControl & 0xe000) != 0) //bits 15-13 are window enable bits
            {
                winMask = _memory.Io.REG_WINOUT & 0x3f;
                if (inWin0YRange && (uint)(x - win0StartX) < win0XThreshold)
                {
                    winMask = _memory.Io.REG_WININ & 0x3f;
                }
                else if (inWin1YRange && (uint)(x - win1StartX) < win1XThreshold)
                {
                    winMask = (_memory.Io.REG_WININ >> 8) & 0x3f;
                }
                else if (objWinEnabled && _objectWindowMask[x] != 0)
                {
                    winMask = (_memory.Io.REG_WINOUT >> 8) & 0x3f;
                }
            }
            ushort hiBgrColor, loBgrColor = hiBgrColor = 0x8000;
            BlendTargetOneType hiColorSource = BlendTargetOneType.Backdrop;
            BlendTargetTwoType loColorSource = BlendTargetTwoType.Backdrop;
            int hiPriorityLine, loPriorityLine = hiPriorityLine = 4;

            foreach (ref readonly var bgIdx in activeBgs)
            {
                if ((winMask & (1 << bgIdx)) == 0) //if not set to display in window continue
                {
                    if (bgIdx == 2) //if 2 update fixed Affine Sources
                    {
                        fixedSourceX += (short)_memory.Io.REG_BG2PA;
                        fixedSourceY += (short)_memory.Io.REG_BG2PC;
                    }
                    continue;
                }

                int paletteIndex;
                bool isSinglePalette = true;
                if (bgIdx == 2) //bg2 handled differently because its affine in this mode
                {
                    //affine
                    paletteIndex = RenderAffineTiledBackground(in vram, ref fixedSourceX,
                        ref fixedSourceY, bgControls[bgIdx], (short)_memory.Io.REG_BG2PA,
                        (short)_memory.Io.REG_BG2PC, bg2Size);
                }
                else
                {
                    //text
                    isSinglePalette = bgScanlineInfos[bgIdx].IsSinglePalette;
                    paletteIndex = RenderTiledTextBackground(in vram, x, hofsTable[bgIdx],
                        bgScanlineInfos[bgIdx].TileMapStartOffset,
                        bgScanlineInfos[bgIdx].XTiles,
                        bgScanlineInfos[bgIdx].CurrentYTile,
                        bgScanlineInfos[bgIdx].PixelYInTile,
                        isSinglePalette,
                        bgScanlineInfos[bgIdx].TileDataStartOffset);
                }

                if (paletteIndex == 0) continue;
                if (!isSinglePalette && (paletteIndex & 0xf) == 0) continue;  //mod 16 in 4bpp mode to cover all palette transparency

                var offset = paletteIndex << 1; // mul 2 bc each palette entry is 2 bytes
                var bgrColor = (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

                if (hiBgrColor == 0x8000)
                {
                    hiBgrColor = bgrColor;
                    hiColorSource = (BlendTargetOneType)(1 << bgIdx);
                    hiPriorityLine = bgControls[bgIdx] & 0b11;
                    continue;
                }

                loBgrColor = bgrColor;
                loColorSource = (BlendTargetTwoType)(1 << (bgIdx + 8));
                loPriorityLine = bgControls[bgIdx] & 0b11;
                break;
            }

            int spriteMode = 0;
            foreach (ref readonly var sprite in visibleSprites)
            {
                if ((winMask & 0x10) == 0) //bit 4 of winMask is to display objects
                {
                    continue;
                }

                int objPaletteIndex = sprite.IsAffine
                    ? RenderAffineSprite(in vram, in sprite, x, loPriorityLine, displayControl)
                    : RenderRegularSprite(in vram, in sprite, x, loPriorityLine);

                if (objPaletteIndex == 0) continue; //if transparent dont draw

                objPaletteIndex += (16 * (sprite.SinglePalette ? 0 : sprite.PaletteNumber));
                var offset = (objPaletteIndex << 1) + 0x200; //object palettes start offset 0x200 in paletteRam
                var objPixelColor = (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

                if (sprite.Priority <= hiPriorityLine) //if sprite has higher priority than current top pixel
                {
                    //make sprite pixel top and next top gets set to previous top
                    (loBgrColor, loColorSource) = (hiBgrColor, hiColorSource.ToBlendTargetTwoType());
                    (hiBgrColor, hiColorSource) = (objPixelColor, BlendTargetOneType.Obj);
                    spriteMode = sprite.Mode;
                    break;
                }

                loBgrColor = objPixelColor;
                loColorSource = BlendTargetTwoType.Obj;
                break;
            }

            if (loBgrColor == 0x8000)
            {
                loBgrColor = (ushort)(paletteRam[0] | (paletteRam[1] << 8));
                if (hiBgrColor == 0x8000)
                {
                    hiBgrColor = loBgrColor;
                }
            }

            if ((winMask & 0x20) != 0) //if window mask bit 5 set then window's special effects enabled
            {
                if (hiColorSource == BlendTargetOneType.Obj && spriteMode == 1)
                {
                    //will always use alpha blending with this as source regardless of BLDCNT
                    var t2BlendingEnabled = (_memory.Io.REG_BLDCNT & (ushort)loColorSource) != 0;
                    if (t2BlendingEnabled)
                    {
                        hiBgrColor = SpecialEffectsHelper.AlphaBlendPixels(hiBgrColor, loBgrColor, _memory.Io.REG_BLDALPHA);
                    }
                    else if (((_memory.Io.REG_BLDCNT >> 6) & 0b11) != 0b00) //blend control bits 6-7 not zero then apply blending
                    {
                        hiBgrColor = ApplyBlendingEffects(hiBgrColor, hiColorSource, loBgrColor, loColorSource);
                    }
                }
                else if (((_memory.Io.REG_BLDCNT >> 6) & 0b11) != 0b00) //blend control bits 6-7 not zero then apply blending
                {
                    hiBgrColor = ApplyBlendingEffects(hiBgrColor, hiColorSource, loBgrColor, loColorSource);
                }
            }

            FrameBuffer.SetPixel(x, y, hiBgrColor);
        }

        _memory.Io.InternalBg2X += (short)_memory.Io.REG_BG2PB;
        _memory.Io.InternalBg2Y += (short)_memory.Io.REG_BG2PD;
    }

    private void RenderMode2(int y)
    {
        if (y == 0)
        {
            _memory.Io.InternalBg2X = BitUtils.SignExtend((int)_memory.Io.REG_BG2X, 28);
            _memory.Io.InternalBg2Y = BitUtils.SignExtend((int)_memory.Io.REG_BG2Y, 28);
            _memory.Io.InternalBg3X = BitUtils.SignExtend((int)_memory.Io.REG_BG3X, 28);
            _memory.Io.InternalBg3Y = BitUtils.SignExtend((int)_memory.Io.REG_BG3Y, 28);
        }

        var displayControl = _memory.Io.REG_DISPCNT;
        var inWin0YRange = SpecialEffectsHelper.TryGetWindowRange(windowNum: 0, y, displayControl,
            _memory.Io.REG_WIN0H,
            _memory.Io.REG_WIN0V,
            out byte win0StartX,
            out uint win0XThreshold);
        var inWin1YRange = SpecialEffectsHelper.TryGetWindowRange(windowNum: 1, y, displayControl,
            _memory.Io.REG_WIN1H,
            _memory.Io.REG_WIN1V,
            out byte win1StartX,
            out uint win1XThreshold);

        var objWinEnabled = (displayControl & 0x8000) != 0; //bit 15 for objWindow enabled
        if (objWinEnabled) _objectWindowMask.AsSpan().Clear(); //clear previous scanLines objWin mask

        var bg2Enabled = BitUtils.IsBitSet(displayControl, 10);
        var bg3Enabled = BitUtils.IsBitSet(displayControl, 11);

        var bg2Control = _memory.Io.REG_BG2CNT;
        var bg3Control = _memory.Io.REG_BG3CNT;

        ReadOnlySpan<ushort> bgControls = [bg2Control, bg3Control];
        Span<int> fixedSourceXTable = [_memory.Io.InternalBg2X, _memory.Io.InternalBg3X];
        Span<int> fixedSourceYTable = [_memory.Io.InternalBg2Y, _memory.Io.InternalBg3Y];
        ReadOnlySpan<ushort> bgPaTable = [_memory.Io.REG_BG2PA, _memory.Io.REG_BG3PA];
        ReadOnlySpan<ushort> bgPcTable = [_memory.Io.REG_BG2PC, _memory.Io.REG_BG3PC];
        ReadOnlySpan<int> bgSizeTable =
        [
            BackgroundHelpers.GetRotationalBackgroundSizePixels((bg2Control >> 14) & 0b11),
            BackgroundHelpers.GetRotationalBackgroundSizePixels((bg3Control >> 14) & 0b11)
        ];

        Span<int> usedBackgrounds = stackalloc int[2];
        int activeBgCount;
        if (bg2Enabled && bg3Enabled)
        {
            activeBgCount = 2;
            var bg3Priority = bg3Control & 0b11;
            var bg2Priority = bg2Control & 0b11;
            if (bg3Priority < bg2Priority)
            {
                usedBackgrounds[0] = 3;
                usedBackgrounds[1] = 2;
            }
            else
            {
                usedBackgrounds[1] = 3;
                usedBackgrounds[0] = 2;
            }
        }
        else if (bg3Enabled)
        {
            activeBgCount = 1;
            usedBackgrounds[0] = 3;
        }
        else if (bg2Enabled)
        {
            activeBgCount = 1;
            usedBackgrounds[0] = 2;
        }
        else
        {
            activeBgCount = 0;
        }
        var activeBgs = usedBackgrounds[..activeBgCount];

        var spriteCount = 0;
        Span<ScanlineSpriteInfo> spriteBuffer = stackalloc ScanlineSpriteInfo[128];
        if ((displayControl & 0x1000) == 0x1000) //bit 12 set means sprites enabled
        {
            spriteCount = GetScanlineVisibleSprites(y, spriteBuffer);
        }

        Span<ScanlineSpriteInfo> visibleSprites = spriteBuffer[..spriteCount];
        SortSpriteIndicesByPriority(visibleSprites);

        ReadOnlySpan<byte> vram = _memory.Vram.AsSpan(0, 0x18000); //Explicit slice with full Length of VRAM to help JIT eliminate bounds checks
        ReadOnlySpan<byte> paletteRam = _memory.PaletteRam.AsSpan(0, 0x400);
        for (int x = 0; x < ScreenWidth; x++)
        {
            var winMask = 0b111111;
            if ((displayControl & 0xe000) != 0) //bits 15-13 are window enable bits
            {
                winMask = _memory.Io.REG_WINOUT & 0x3f;
                if (inWin0YRange && (uint)(x - win0StartX) < win0XThreshold)
                {
                    winMask = _memory.Io.REG_WININ & 0x3f;
                }
                else if (inWin1YRange && (uint)(x - win1StartX) < win1XThreshold)
                {
                    winMask = (_memory.Io.REG_WININ >> 8) & 0x3f;
                }
                else if (objWinEnabled && _objectWindowMask[x] != 0)
                {
                    winMask = (_memory.Io.REG_WINOUT >> 8) & 0x3f;
                }
            }
            ushort hiBgrColor, loBgrColor = hiBgrColor = 0x8000;
            BlendTargetOneType hiColorSource = BlendTargetOneType.Backdrop;
            BlendTargetTwoType loColorSource = BlendTargetTwoType.Backdrop;
            int hiPriorityLine, loPriorityLine = hiPriorityLine = 4; //priority of hi and lo pixel is defaulted to 4 which is always higher than any bg or obj pixel

            foreach (ref readonly var bgIdx in activeBgs)
            {
                if ((winMask & (1 << bgIdx)) == 0) //if not set to display in window continue
                {
                    ref var sourceXFixed = ref fixedSourceXTable[bgIdx - 2];
                    ref var sourceYFixed = ref fixedSourceYTable[bgIdx - 2];
                    sourceXFixed += (short)bgPaTable[bgIdx - 2];
                    sourceYFixed += (short)bgPcTable[bgIdx - 2];
                    continue;
                }

                //Mode 2 bgs are always 8bpp aka singlePaletteMode
                var bgControl = bgControls[bgIdx - 2];
                var paletteIndex = RenderAffineTiledBackground(in vram, ref fixedSourceXTable[bgIdx - 2],
                    ref fixedSourceYTable[bgIdx - 2], bgControl, (short)bgPaTable[bgIdx - 2],
                    (short)bgPcTable[bgIdx - 2], bgSizeTable[bgIdx - 2]);

                if (paletteIndex == 0) //transparent
                {
                    continue;
                }

                var offset = paletteIndex << 1; // mul 2 bc each palette entry is 2 bytes
                var bgrColor = (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

                if (hiBgrColor == 0x8000)
                {
                    hiBgrColor = bgrColor;
                    hiColorSource = (BlendTargetOneType)(1 << bgIdx);
                    hiPriorityLine = bgControls[bgIdx - 2] & 0b11;
                    continue;
                }

                loBgrColor = bgrColor;
                loColorSource = (BlendTargetTwoType)(1 << (bgIdx + 8));
                loPriorityLine = bgControls[bgIdx - 2] & 0b11;
                break;
            }

            int spriteMode = 0;
            foreach (ref readonly var sprite in visibleSprites)
            {
                if ((winMask & 0x10) == 0) //bit 4 of winMask is to display objects
                {
                    continue;
                }

                int objPaletteIndex = sprite.IsAffine
                    ? RenderAffineSprite(in vram, in sprite, x, loPriorityLine, displayControl)
                    : RenderRegularSprite(in vram, in sprite, x, loPriorityLine);

                if (objPaletteIndex == 0)
                {
                    continue;
                }

                objPaletteIndex += (16 * (sprite.SinglePalette ? 0 : sprite.PaletteNumber));
                var offset = (objPaletteIndex << 1) + 0x200; //object palettes start offset 0x200 in paletteRam
                var objPixelColor = (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

                if (sprite.Priority <= hiPriorityLine) //if sprite has higher priority than current top pixel
                {
                    //make sprite pixel top and next top gets set to previous top
                    (loBgrColor, loColorSource) = (hiBgrColor, hiColorSource.ToBlendTargetTwoType());
                    (hiBgrColor, hiColorSource) = (objPixelColor, BlendTargetOneType.Obj);
                    spriteMode = sprite.Mode;
                    break;
                }

                loBgrColor = objPixelColor;
                loColorSource = BlendTargetTwoType.Obj;
                break;
            }

            if (loBgrColor == 0x8000)
            {
                loBgrColor = (ushort)(paletteRam[0] | (paletteRam[1] << 8));
                if (hiBgrColor == 0x8000)
                {
                    hiBgrColor = loBgrColor;
                }
            }

            if ((winMask & 0x20) != 0) //if window mask bit 5 set then window's special effects enabled
            {
                if (hiColorSource == BlendTargetOneType.Obj && spriteMode == 1)
                {
                    //will always use alpha blending with this as source regardless of BLDCNT
                    var t2BlendingEnabled = (_memory.Io.REG_BLDCNT & (ushort)loColorSource) == (ushort)loColorSource;
                    if (t2BlendingEnabled)
                    {
                        hiBgrColor = SpecialEffectsHelper.AlphaBlendPixels(hiBgrColor, loBgrColor, _memory.Io.REG_BLDALPHA);
                    }
                    else if (((_memory.Io.REG_BLDCNT >> 6) & 0b11) != 0b00) //blend control bits 6-7 not zero then apply blending
                    {
                        hiBgrColor = ApplyBlendingEffects(hiBgrColor, hiColorSource, loBgrColor, loColorSource);
                    }
                }
                else if (((_memory.Io.REG_BLDCNT >> 6) & 0b11) != 0b00) //blend control bits 6-7 not zero then apply blending
                {
                    hiBgrColor = ApplyBlendingEffects(hiBgrColor, hiColorSource, loBgrColor, loColorSource);
                }
            }

            FrameBuffer.SetPixel(x, y, hiBgrColor);
        }

        _memory.Io.InternalBg2X += (short)_memory.Io.REG_BG2PB;
        _memory.Io.InternalBg2Y += (short)_memory.Io.REG_BG2PD;
        _memory.Io.InternalBg3X += (short)_memory.Io.REG_BG3PB;
        _memory.Io.InternalBg3Y += (short)_memory.Io.REG_BG3PD;
    }

    private static int RenderAffineTiledBackground(in ReadOnlySpan<byte> vram, ref int sourceXFixed, ref int sourceYFixed, ushort bgControl, short pa, short pc, int backgroundSize)
    {
        int sourceX = sourceXFixed >> 8; // div 256
        int sourceY = sourceYFixed >> 8; // div 256

        sourceXFixed += pa;
        sourceYFixed += pc;

        var wrapAround = BitUtils.IsBitSet(bgControl, 13);
        if (wrapAround)
        {
            sourceX &= backgroundSize - 1; //mod by bgSize
            sourceY &= backgroundSize - 1; //mod by bgSize
        }
        else if ((uint)sourceX >= (uint)backgroundSize ||
                 (uint)sourceY >= (uint)backgroundSize)
        {
            return 0;
        }
        // calc pixel color with sourceX and sourceY
        var charBaseBlock = (bgControl >> 2) & 0b11; //bgXcnt bits 2-3
        var charDataStartOffset = charBaseBlock * 0x4000;
        var screenBaseBlock = (bgControl >> 8) & 0x1F; //bgXcnt bits 8-12
        var tileMapStartOffset = screenBaseBlock * 0x800;

        //tileMapIndex = tileY * tilesPerRow + xTile
        var tileMapIndex = ((sourceY >> 3) * (backgroundSize >> 3)) + (sourceX >> 3);
        var tileNumber = vram[tileMapStartOffset + tileMapIndex];

        var currentTileStartOffset = charDataStartOffset + tileNumber * 0x40; //tile size 0x40 bc mode2 is forced 8bpp for bgs
        var yPixel = sourceY & 7; //mod 8
        var xPixel = sourceX & 7; //mod 8

        var paletteIndex = vram[currentTileStartOffset + yPixel * 8 + xPixel];
        return paletteIndex;
    }

    private int GetScanlineVisibleSprites(int y, Span<ScanlineSpriteInfo> sprites)
    {
        int count = 0;
        ReadOnlySpan<byte> oam = _memory.Oam.AsSpan(0, 0x400); //Explicit slice with full Length of OAM to help JIT eliminate bounds checks

        for (int oamAttrOffset = 0; oamAttrOffset < 1016; oamAttrOffset += 8) //loop runs for sprites 0-127
        {
            var attr0Value = Read16(in oam, oamAttrOffset);
            var attr0 = new ObjAttribute0(attr0Value);
            var isSinglePalette = attr0.IsSinglePalette; //just here for later so I remember
            if (attr0 is { IsRotationScaling: false, IsDisabled: true })
            {
                continue; //disabled but only if not r/s obj otherwise its IsDoubleSize
            }

            var attr1Value = Read16(in oam, oamAttrOffset + 2);
            var attr2Value = Read16(in oam, oamAttrOffset + 4);
            if (attr0.IsRotationScaling)
            {
                var rotateParamGroup = (attr1Value >> 9) & 0x1f;
                var paStartOffset = 0x6 + (rotateParamGroup * 0x20);
                var pA = (short)Read16(in oam, paStartOffset);
                var pB = (short)Read16(in oam, paStartOffset + 8);
                var pC = (short)Read16(in oam, paStartOffset + 16);
                var pD = (short)Read16(in oam, paStartOffset + 24);
                var doubleSized = attr0.IsDisabled;
                var affattr1 = new ObjAttribute1(attr1Value);

                var affshapeSize = (attr0.ObjShape << 2) | affattr1.ObjSize; //low two bits attr1 size
                SpriteHelpers.GetSpriteSizeTiles(affshapeSize, out int affxTiles, out int affyTiles);

                int spriteCanvasHeight = affyTiles * 8;
                if (doubleSized)
                {
                    spriteCanvasHeight *= 2;
                }

                int sY = attr0.YCoord;
                if (sY + spriteCanvasHeight > 256)
                {
                    sY -= 256; //Y wrapping
                }

                var canvasY = y - sY;
                if ((uint)canvasY >= (uint)spriteCanvasHeight) //not in Y range
                {
                    continue;
                }

                int relativeY = canvasY - spriteCanvasHeight / 2;
                var affattr2 = new ObjAttribute2(attr2Value);

                var rotationalSprite = new ScanlineSpriteInfo(affattr2.TileNumber, isSinglePalette, affattr2.PaletteNumber,
                    relativeY, affattr2.Priority, affxTiles, affattr1.XCoord, attr0.ObjMode, doubleSized, true, pA, pB,
                    pC, pD, affyTiles);

                if (attr0.ObjMode == 2) //obj window
                {
                    RasterizeObjWindowSprite(in rotationalSprite, _memory.Io.REG_DISPCNT);
                    continue;
                }

                sprites[count++] = rotationalSprite;
                continue;
            }

            var attr1 = new ObjAttribute1(attr1Value);

            var shapeSize = (attr0.ObjShape << 2) | attr1.ObjSize; //low two bits attr1 size
            SpriteHelpers.GetSpriteSizeTiles(shapeSize, out int xTiles, out int yTiles);

            //only * 8 for 8bpp mode in 4bpp mode its * 4 since each byte represents 2 pix of a tile
            var spriteY = attr0.YCoord;
            var canvasHeight = yTiles * 8;
            if (spriteY + canvasHeight > 256)
            {
                spriteY -= 256; //Y wrap around screen
            }

            int spriteYPos = y - spriteY;
            bool objYRange = (uint)spriteYPos < (uint)canvasHeight;

            if (!objYRange)
            {
                continue;
            }

            //var attr2Value = Read16(oam, oamAttrOffset + 4);
            var attr2 = new ObjAttribute2(attr2Value);

            var currentYTile = spriteYPos >> 3; //div 8
            var currentYPixel = spriteYPos & 7; //mod 8
            if (attr1.VerticalMirrored)
            {
                currentYTile = yTiles - currentYTile;
                currentYPixel = 7 - currentYPixel;
            }

            var yPixelOffset = currentYPixel * 4; //mul 4(4bppMode) for pixel inside tile offset
            var startTileNumber = attr2.TileNumber;
            var twoDMatrixSize = 32;

            if (isSinglePalette)
            {
                startTileNumber /= 2;
                yPixelOffset *= 2; //mul 2 (8 total bc already mul 4 above) in 8bpp mode because each byte is a pixel
                twoDMatrixSize = 16;
            }

            int scanlineStartMapTileNumber;
            if ((_memory.Io.REG_DISPCNT & 0x40) == 0x40) //bit 6 set then 1d char mapping
            {
                scanlineStartMapTileNumber = startTileNumber + (currentYTile * xTiles);
            }
            else //bit 6 clear 2d character mapping
            {
                scanlineStartMapTileNumber = startTileNumber + (currentYTile * twoDMatrixSize);
            }

            var regSprite = new ScanlineSpriteInfo(scanlineStartMapTileNumber, isSinglePalette, attr2.PaletteNumber,
                yPixelOffset, attr2.Priority, xTiles, attr1.XCoord, attr0.ObjMode, attr1.HorizontalMirrored, false, 0,
                0, 0, 0, 0);

            if (attr0.ObjMode == 2) //obj window
            {
                RasterizeObjWindowSprite(in regSprite, _memory.Io.REG_DISPCNT);
                continue;
            }

            //add to list for display reg sprites
            sprites[count++] = regSprite;
        }

        return count;
    }

    private readonly byte[] _objectWindowMask = new byte[ScreenWidth];
    private void RasterizeObjWindowSprite(in ScanlineSpriteInfo sprite, ushort displayControl)
    {
        int canvasWidth = sprite.XTiles * 8;

        if (sprite is { IsAffine: true, DoubleSize: true }) //for now hFlip is DoubleSize flag for rotationalSprites
        {
            canvasWidth *= 2;
        }

        int spriteX = sprite.XCoord;

        int startX = Math.Max(spriteX, 0);
        int endX = Math.Min(spriteX + canvasWidth, ScreenWidth);
        ReadOnlySpan<byte> vram = _memory.Vram.AsSpan(0, 0x18000);
        for (int x = startX; x < endX; x++)
        {
            int paletteIndex = sprite.IsAffine
                ? SampleAffineSpritePixel(in vram, in sprite, x, displayControl, canvasWidth)
                : SampleRegularSpritePixel(in vram, in sprite, x);

            if (paletteIndex != 0)
            {
                _objectWindowMask[x] = 1;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort Read16(in ReadOnlySpan<byte> memoryRegion, int offset)
    {
        return (ushort)((memoryRegion[offset + 1] << 8) | memoryRegion[offset]);
    }

    private void RenderMode3(int y)
    {
        ReadOnlySpan<byte> vram = _memory.Vram.AsSpan(0, 0x18000);
        for (var x = 0; x < ScreenWidth; x++)
        {
            var offset = ((y * ScreenWidth) + x) * 2;

            var bgr555 = Read16(in vram, offset);
            FrameBuffer.SetPixel(x, y, bgr555);
        }
    }

    private void RenderMode4(int y)
    {
        var useFrame1 = (_memory.Io.REG_DISPCNT & 0x10) != 0; //bit 4 of displayControl tells which bitmap to use
        var vram = useFrame1
            ? _memory.Vram.AsSpan()[0xA000..]
            : _memory.Vram.AsSpan()[..0xA000];
        var paletteRam = _memory.PaletteRam.AsSpan(0, 0x400);
        var pixelRowOffset = y * ScreenWidth;
        for (var x = 0; x < ScreenWidth; x++)
        {
            var vramPixelOffset = pixelRowOffset + x;
            var paletteIndex = vram[vramPixelOffset];
            var offset = paletteIndex << 1;

            var color =  (ushort)(paletteRam[offset] | (paletteRam[offset + 1] << 8));

            FrameBuffer.SetPixel(x, y, color);
        }
    }

    private void RenderMode5(int y)
    {
        var paletteRam = _memory.PaletteRam.AsSpan(0, 0x400);
        if (y > 127) //bitmap is only covering 128 scanLines
        {
            var backDropColor = (ushort)(paletteRam[0] | (paletteRam[1] << 8));
            FrameBuffer.FillScanline(y, backDropColor);
            return;
        }

        var useFrame1 = (_memory.Io.REG_DISPCNT & 0x10) != 0; //bit 4 of displayControl tells which bitmap to use
        var startOffset = !useFrame1 ? 0 : 0xA000;
        var pixelRowOffset = y * 160;
        ReadOnlySpan<byte> vram = _memory.Vram.AsSpan(0, 0x18000);
        for (int x = 0; x < ScreenWidth; x++)
        {
            if (x > 159) //bitmap is only 160 pixels across
            {
                var backDropColor = (ushort)(paletteRam[0] | (paletteRam[1] << 8));
                FrameBuffer.SetPixel(x, y, backDropColor);
                continue;
            }

            var offset = startOffset + ((pixelRowOffset + x) << 1);

            var bgr555 = Read16(in vram, offset);
            FrameBuffer.SetPixel(x, y, bgr555);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastSortBackgroundsByPriority(uint displayControl, ReadOnlySpan<ushort> bgControlRegs,
        Span<int> bgIndexOutput, uint validBackgroundMask)
    {
        int count = 0;
        int p0 = 0xff, p1 = 0xff, p2 = 0xff, p3 = 0xff;

        uint enabledMask = ((displayControl >> 8) & 0xf) & validBackgroundMask;
        if ((enabledMask & 1) != 0) p0 = ((bgControlRegs[0] & 0b11) << 2);
        if ((enabledMask & 2) != 0) p1 = ((bgControlRegs[1] & 0b11) << 2) | 1;
        if ((enabledMask & 4) != 0) p2 = ((bgControlRegs[2] & 0b11) << 2) | 2;
        if ((enabledMask & 8) != 0) p3 = ((bgControlRegs[3] & 0b11) << 2) | 3;

        if (p0 > p1) { (p0, p1) = (p1, p0); }
        if (p2 > p3) { (p2, p3) = (p3, p2); }
        if (p0 > p2) { (p0, p2) = (p2, p0); }
        if (p1 > p3) { (p1, p3) = (p3, p1); }
        if (p1 > p2) { (p1, p2) = (p2, p1); }

        int idx = 0;
        if (p0 != 0xff) { bgIndexOutput[idx++] = p0 & 0b11; count++; }
        if (p1 != 0xff) { bgIndexOutput[idx++] = p1 & 0b11; count++; }
        if (p2 != 0xff) { bgIndexOutput[idx++] = p2 & 0b11; count++; }

        if (p3 == 0xff)
        {
            return count;
        }

        bgIndexOutput[idx] = p3 & 0b11; count++;
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SortSpriteIndicesByPriority(Span<ScanlineSpriteInfo> sprites)
    {
        if (sprites.Length <= 1) return; //No need to sort single or empty sprite list

        Span<int> offsets = stackalloc int[4];

        foreach (ref readonly var sprite in sprites)
        {
            offsets[sprite.Priority]++;
        }

        int p0Count = offsets[0];
        int p1Count = offsets[1];
        int p2Count = offsets[2];

        offsets[0] = 0;
        offsets[1] = p0Count;
        offsets[2] = p0Count + p1Count;
        offsets[3] = p0Count + p1Count + p2Count;

        Span<ScanlineSpriteInfo> tempOutput = stackalloc ScanlineSpriteInfo[128];

        foreach (ref readonly var sprite in sprites)
        {
            tempOutput[offsets[sprite.Priority]++] = sprite;
        }

        tempOutput[..sprites.Length].CopyTo(sprites);
    }
}