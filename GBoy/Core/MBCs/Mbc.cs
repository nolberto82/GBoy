namespace GBoy.Core.MBCs;
public abstract class MBC
{
    public byte[] Rom { get; set; }
    public int RomBank { get; set; }
    public int RamBank { get; set; }
    public bool CartRamOn { get; set; }
    public bool CGB { get; set; }

    public abstract void Init(byte[] rom, bool cgb);
    public abstract byte ReadRom0(int a);
    public abstract byte ReadRom1(int a);
    public abstract Span<byte> ReadRomBlock(int a, int size);
    public abstract void WriteRom0(int a, byte v, bool edit = false);
    public abstract void WriteRom1(int a, byte v, bool edit = false);

    public void SetRomBank(int number) => RomBank = number;
}