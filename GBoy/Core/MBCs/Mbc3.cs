namespace GBoy.Core.MBCs;
public class Mbc3 : MBC
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
        var addr = a + (0x4000 * (RomBank - 1));
        if (RomBank > 1)
            return Rom[addr];
        else
            return Rom[addr];
    }

    public override Span<byte> ReadRomBlock(int a, int size)
    {
        if (a <= 0x3fff)
            return new(Rom, a, size);
        else
            return new(Rom, a + 0x4000 * (RomBank - 1), size);
    }

    public override void WriteRom0(int a, byte v, bool edit = false)
    {
        if (edit)
            Rom[a] = v;
        else
        {
            if (a <= 0x1fff)
                CartRamOn = v == 0x0a ? true : false;
            else if (a <= 0x3fff)
                RomBank = v == 0 ? 1 : v & 0x7f;
        }
    }

    public override void WriteRom1(int a, byte v, bool edit = false)
    {
        if (edit)
            Rom[a + (0x4000 * (RomBank - 1))] = v;
        else
        {
            if (a <= 0x5fff)
                RamBank = v & 3;
        }
    }
}
