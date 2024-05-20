﻿
namespace GBoy.Core.Sound;
public class Square2 : BaseChannel
{
    public byte NR21 { get; private set; }
    public byte NR22 { get; private set; }
    public byte NR23 { get; private set; }
    public byte NR24 { get; private set; }

    public Square2() { }
    public Square2(Mmu mmu) => Mmu = mmu;

    public override void Write(int a, byte v)
    {
        if (a == 0x16)
        {
            Duty = (v & 0xc0) >> 6;
            LengthCounter = 64 - (v & 0x3f);
            NR21 = v;
        }
        else if (a == 0x17)
        {
            EnvVolume = (v & 0xf0) >> 4;
            EnvDirection = (v & 0x08) > 0;
            EnvPeriod = v & 0x07;
            Dac = (v & 0xf8) > 0;
            NR22 = v;
        }
        else if (a == 0x18)
        {
            Frequency = Frequency & 0xff00 | v;
            NR23 = v;
        }
        else if (a == 0x19)
        {
            Frequency = (Frequency & 0xff) | (v & 0x07) << 8;
            LengthEnabled = v.GetBit(6);
            if (v.GetBit(7))
                Trigger(64, 4);
            NR24 = v;
        }
    }

    public override byte Read(int a)
    {
        if (a == 0x16)
            return (byte)(NR21 | 0x3f);
        else if (a == 0x17)
            return NR22;
        else if (a == 0x18)
            return (byte)(NR23 | 0xff);
        else if (a == 0x19)
            return (byte)(NR24 | 0xbf);
        return 0xff;
    }

    public override void Reset()
    {
        Frequency = 0;
        LengthCounter = 0;
        Duty = 0;
        EnvVolume = 0;
        Timer = 0;

        NR21 = 0x3f;
        NR22 = 0x00;
        NR23 = 0xff;
        NR24 = 0xbf;
        Dac = false;
        Enabled = false;
    }

    public override List<byte> Save()
    {
        return
        [
            ..Frequency.GetBytes(),
            ..LengthCounter.GetBytes(),
            ..Duty.GetBytes(),
            ..EnvVolume.GetBytes(),
            ..CurrentVolume.GetBytes(),
            ..Timer.GetBytes(),
            NR21,
            NR22,
            NR23,
            NR24,
        ];
    }

    public override void Load(BinaryReader br)
    {
        Frequency = br.ReadInt32();
        LengthCounter = br.ReadInt32();
        Duty = br.ReadInt32();
        EnvVolume = br.ReadInt32();
        CurrentVolume = br.ReadInt32();
        Timer = br.ReadInt32();
        NR21 = br.ReadByte();
        NR22 = br.ReadByte();
        NR23 = br.ReadByte();
        NR24 = br.ReadByte();
    }
}
