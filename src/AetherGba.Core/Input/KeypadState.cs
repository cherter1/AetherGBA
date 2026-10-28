using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Input;

public sealed class KeypadState
{
    private readonly GbaMemory _memory;
    private readonly InterruptController _interrupts;

    public KeypadState(GbaMemory memory, InterruptController interrupts)
    {
        _memory = memory;
        _interrupts = interrupts;
    }

    public void SetPressed(GbaButton button, bool pressed)
    {
        var mask = (ushort)(1 << (int)button);

        if (pressed)
        {
            _memory.Io.REG_KEYINPUT = (ushort)(_memory.Io.REG_KEYINPUT & ~mask);

            var generateInterrupts = (_memory.Io.REG_KEYCNT & 0x4000) != 0; //bit 14
            if (!generateInterrupts)
            {
                return;
            }

            var interruptMask = _memory.Io.REG_KEYCNT & 0x3ff;
            var andOperation = (_memory.Io.REG_KEYCNT & 0x8000) != 0;
            if (andOperation) //All Set Keys Pressed
            {
                //TODO: handling simultaneous input
            }
            else //Any set key pressed
            {
                if ((interruptMask & mask) != 0)
                {
                    _interrupts.Request(InterruptType.Keypad);
                }
            }
        }
        else //released
        {
            _memory.Io.REG_KEYINPUT = (ushort)(_memory.Io.REG_KEYINPUT | mask);
        }
    }
}