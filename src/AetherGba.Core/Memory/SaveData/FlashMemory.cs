namespace AetherGba.Core.Memory.SaveData;

public sealed class FlashMemory
{
    private const uint SaveBase = 0x0e000000;
    private const int BankSize = 0x10000;
    private const int SectorSize = 0x1000;

    private const int UnlockOffset1 = 0x5555;
    private const int UnlockOffset2 = 0x2aaa;

    public readonly byte[] Memory = new byte[0x020000]; //FLASH1M

    private readonly string _saveFileName;
    private FlashState _state;
    private bool _idMode;
    private bool _dirty;

    private int _bank; //0 or 1 (only for devices bigger than 64k or 0x10000)

    public FlashMemory(string saveFile)
    {
        _saveFileName = saveFile;
        Array.Fill<byte>(Memory, 0xff);

        if (!File.Exists(_saveFileName))
            return;

        byte[] saveData = File.ReadAllBytes(_saveFileName);

        Array.Copy(saveData, Memory, Math.Min(saveData.Length, Memory.Length));
    }

    ~FlashMemory()
    {
        FlushSave(); //maybe unreliable maybe change when file flush happens
    }

    public byte Read8(uint address)
    {
        if (!TryGetOffset(address, out var offset))
            return 0xff;

        if (_idMode)
        {
            return offset switch
            {
                0 => 0x62, //Sanyo manufacturer code
                1 => 0x13, //Sanyo deviceCode code
                _ => Memory[_bank * BankSize + offset]
            };
        }
        return Memory[_bank * BankSize + offset];
    }

    public void Write8(uint address, byte value)
    {
        if (!TryGetOffset(address, out var offset))
            return;

        switch (_state)
        {
            case  FlashState.Ready:
                if (offset == UnlockOffset1 && value == 0xaa)
                    _state = FlashState.Unlock1;
                break;

            case FlashState.Unlock1:
                if (offset == UnlockOffset2 && value == 0x55)
                    _state = FlashState.Unlock2;
                break;

            case FlashState.Unlock2: // do command
                _state = FlashState.Ready;

                if (offset != UnlockOffset1)
                    break;

                switch (value) //command value
                {
                    case 0x90: //Enter Id mode
                        _idMode = true;
                        break;
                    case 0xF0: //terminate Id mode
                        _idMode = false;
                        break;
                    case 0x80: //Begin erase sequence
                        _state = FlashState.EraseUnlock1;
                        break;
                    case 0xa0: //write byte command
                        _state = FlashState.ProgramData;
                        break;
                    case 0xb0: //select bank command
                        _state = FlashState.BankData;
                        break;
                }
                break;
            case FlashState.EraseUnlock1:
                _state = offset == UnlockOffset1 && value == 0xaa
                    ? FlashState.EraseUnlock2
                    : FlashState.Ready;
                break;
            case FlashState.EraseUnlock2:
                _state = offset == UnlockOffset2 && value == 0x55
                    ? FlashState.EraseCommand
                    : FlashState.Ready;
                break;
            case FlashState.EraseCommand:
                _state = FlashState.Ready;

                switch (value)
                {
                    case 0x10 when offset == UnlockOffset1: //Erase full chip, both banks
                        Array.Fill(Memory, (byte)0xff);
                        _dirty = true;
                        break;
                    case 0x30: //Erase sector
                        int sectorStart = _bank * BankSize + (offset & ~(SectorSize - 1)); //start offset of sector to erase

                        Array.Fill(Memory, (byte)0xff, sectorStart, SectorSize);
                        _dirty = true;
                        break;
                }
                break;
            case FlashState.ProgramData:
                _state = FlashState.Ready;

                Memory[_bank * BankSize + offset] = value;
                _dirty = true;
                break;
            case FlashState.BankData:
                _state = FlashState.Ready;

                if (offset == 0 && value < 2)
                    _bank = value;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(_state));
        }
    }

    public void FlushSave()
    {
        if (!_dirty)
            return;

        Console.WriteLine("Flushing save");
        string? dir = Path.GetDirectoryName(_saveFileName);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        string tempFile = _saveFileName + ".tmp";
        File.WriteAllBytes(tempFile, Memory);
        File.Move(tempFile, _saveFileName, overwrite: true);

        _dirty = false;
    }

    private static bool TryGetOffset(uint address, out int offset)
    {
        if (address is < SaveBase or >= SaveBase + BankSize)
        {
            offset = 0;
            return false;
        }

        offset = (int)(address - 0x0e000000);
        return true;
    }
}

public enum FlashState
{
    Ready,
    Unlock1,
    Unlock2,
    EraseUnlock1,
    EraseUnlock2,
    EraseCommand,
    ProgramData,
    BankData
}