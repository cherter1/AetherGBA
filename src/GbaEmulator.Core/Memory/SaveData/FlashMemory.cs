namespace GbaEmulator.Core.Memory.SaveData;

public sealed class FlashMemory
{
    public readonly byte[] Memory = new byte[0x020000]; //FLASH1M

    public FlashMemory(string saveFile)
    {
        _saveFileName = saveFile;
        Array.Fill<byte>(Memory, 0xff);
    }

    private readonly string _saveFileName;

    //only can use 8 bit read writes
    private bool _idMode;
    private bool _writeMode = false;
    private FlashUnlockState _unlockState = FlashUnlockState.InitialLocked;

    public int Bank = 0; //0 or 1 (only for devices bigger than 64k or 0x10000)

    public byte Read8(uint address)
    {
        if (_idMode)
        {
            switch (address)
            {
                //Sanyo dev man
                case 0x0e000000: return 0x62; //manufacturer code
                case 0x0e000001: return 0x13; //deviceCode code
            }
        }
        return 0xff;
    }

    public void Write8(uint address, byte value)
    {
        switch (_unlockState)
        {
            case  FlashUnlockState.InitialLocked:
                if (address == 0x0e005555 && value == 0xaa)
                {
                    _unlockState = FlashUnlockState.Step2Locked;
                }
                break;
            case FlashUnlockState.Step2Locked:
                if (address == 0x0e002aaa && value == 0x55)
                {
                    _unlockState = FlashUnlockState.Unlocked;
                }
                break;
            case FlashUnlockState.Unlocked: // do command
                if (address == 0x0e005555)
                {
                    switch (value) //command value
                    {
                        case 0x90: //Enter Id mode
                            _idMode = true;
                            break;
                        case 0xF0: //terminate Id mode
                            _idMode = false;
                            break;
                        case 0x80: //erase command
                            break;
                        case 0x10: //erase entire chip
                            break;
                        case 0x30: //erase 4k sector
                            break;
                        case 0xa0: //write byte
                            break;
                        case 0xb0: //select bank command
                            break;
                    }
                }
                _unlockState = FlashUnlockState.InitialLocked;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(_unlockState));
        }
        //do later
    }
}

public enum FlashUnlockState
{
    InitialLocked,
    Step2Locked,
    Unlocked
}