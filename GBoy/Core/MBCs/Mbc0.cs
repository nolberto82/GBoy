namespace GBoy.Core.MBCs;
public class MBC0 : MBC
{
    public override void Init(byte[] rom, bool cgb)
    {
        Rom = rom;
        CGB = cgb;
        RomBank = 1;
    }

    public override byte ReadRom0(int a)
    {
        return Rom[a];
    }

    public override byte ReadRom1(int a)
    {
        return Rom[a];
    }

    public override Span<byte> ReadRomBlock(int a, int s)
    {
        return new();
    }

    public override void WriteRom0(int a, byte v, bool edit = false)
    {
        if (edit)
            Rom[a] = v;
    }

    public override void WriteRom1(int a, byte v, bool edit = false)
    {
        if (edit)
            Rom[a] = v;
    }
}
