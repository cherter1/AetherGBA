namespace AetherGba.Core.Cpu;

public sealed class RegisterBank
{
    public ProgramStatusRegister Cpsr = new() { Mode = CpuMode.System, IrqDisable = true };

    private readonly uint[] _shared = new uint[16];
    //fiq mode banks registers 8-12 so index[0] here is r8_fiq and so on
    private readonly uint[] _fiqBankedRegisters = new uint[5];

    private uint _spUserSystem;
    private uint _lrUserSystem;

    private uint _spFiq;
    private uint _lrFiq;
    private ProgramStatusRegister _spsrFiq;

    private uint _spIrq;
    private uint _lrIrq;
    private ProgramStatusRegister _spsrIrq;

    private uint _spSvc;
    private uint _lrSvc;
    private ProgramStatusRegister _spsrSvc;


    public uint this[int index]
    {
        get => index switch
        {
            >= 8 and <= 12 when Cpsr.Mode == CpuMode.Fiq => _fiqBankedRegisters[index - 8],
            13 => StackPointer,
            14 => LinkRegister,
            _ => _shared[index]
        };
        set
        {
            switch (index)
            {
                case >= 8 and <= 12 when Cpsr.Mode == CpuMode.Fiq:
                    _fiqBankedRegisters[index - 8] = value;
                    break;
                case 13:
                    StackPointer = value;
                    break;
                case 14:
                    LinkRegister = value;
                    break;
                default:
                    _shared[index] = value;
                    break;
            }
        }
    }

    public uint StackPointer
    {
        get => Cpsr.Mode switch
        {
            CpuMode.Fiq => _spFiq,
            CpuMode.Irq => _spIrq,
            CpuMode.Supervisor => _spSvc,
            _ => _spUserSystem
        };
        private set
        {
            switch (Cpsr.Mode)
            {
                case CpuMode.Fiq:
                    _spFiq = value;
                    break;
                case CpuMode.Irq:
                    _spIrq = value;
                    break;
                case CpuMode.Supervisor:
                    _spSvc = value;
                    break;
                case CpuMode.User:
                case CpuMode.System:
                case CpuMode.Abort:
                case CpuMode.Undefined:
                default:
                    _spUserSystem = value;
                    break;
            }
        }
    }
    public uint LinkRegister
    {
        get => Cpsr.Mode switch
        {
            CpuMode.Fiq => _lrFiq,
            CpuMode.Irq => _lrIrq,
            CpuMode.Supervisor => _lrSvc,
            _ => _lrUserSystem
        };
        private set
        {
            switch (Cpsr.Mode)
            {
                case CpuMode.Fiq:
                    _lrFiq = value;
                    break;
                case CpuMode.Irq:
                    _lrIrq = value;
                    break;
                case CpuMode.Supervisor:
                    _lrSvc = value;
                    break;
                case CpuMode.User:
                case CpuMode.System:
                case CpuMode.Abort:
                case CpuMode.Undefined:
                default:
                    _lrUserSystem = value;
                    break;
            }
        }
    }
    public uint ProgramCounter
    {
        get => _shared[15];
        set => _shared[15] = value;
    }

    public ProgramStatusRegister GetSpsr() => Cpsr.Mode switch
    {
        CpuMode.Fiq => _spsrFiq,
        CpuMode.Irq => _spsrIrq,
        CpuMode.Supervisor => _spsrSvc,
        _ => throw new InvalidOperationException($"Mode {Cpsr.Mode} has no SPSR")
    };

    public void SetSpsr(CpuMode mode, ProgramStatusRegister value)
    {
        switch (mode)
        {
            case CpuMode.Fiq:
                _spsrFiq = value;
                break;
            case CpuMode.Irq:
                _spsrIrq = value;
                break;
            case CpuMode.Supervisor:
                _spsrSvc = value;
                break;
            default:
                throw new InvalidOperationException($"Mode {mode} has no SPSR");
        }
    }

    public void InitializeForGba()
    {
        // BIOS default stacks commonly used on GBA
        _spUserSystem = 0x03007F00;
        _spFiq = 0;
        _spIrq = 0x03007FA0;
        _spSvc = 0x03007FE0;

        _lrUserSystem = 0;
        _lrFiq = 0;
        _lrIrq = 0;
        _lrSvc = 0;
    }
}