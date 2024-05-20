using GBoy.Core.MBCs;
using GBoy.Gui;

namespace GBoy.Core;
public class Mmu : SaveState
{
    public byte[] Ram { get; private set; }
    public byte[] Rom { get; private set; }
    public byte[] Vram { get; private set; }
    public byte[] Sram { get; private set; }
    public byte[] Wram { get; private set; }

    public bool RomLoaded { get; private set; }
    public string RomName { get; private set; }

    public Cpu Cpu { get; private set; }
    public MBC Mbc { get; private set; }
    public IO IO { get; private set; }
    public Breakpoint Breakpoint { get; private set; }

    public Mmu(IO io, Breakpoint breakpoint)
    {
        Ram = new byte[0x10000];
        Vram = new byte[0x4000];
        Sram = new byte[0x8000];
        Wram = new byte[0x8000];

        IO = io;
        Breakpoint = breakpoint;
    }

    public void Init(Cpu cpu, MBC mbc)
    {
        Cpu = cpu;
        Mbc = mbc;
    }

    public byte Read(int a, bool savestate = false)
    {
        a &= 0xffff;
        byte v = 0xff;
        if (a <= 0x7fff)
        {
            if (a <= 0x3fff)
                v = Mbc.ReadRom0(a);
            else
                v = Mbc.ReadRom1(a);
            ProcessCheats(a, ref v);
            return v;
        }
        else if (a <= 0x9fff)
            return Vram[a - 0x8000 + (0x2000 * IO.VBK)];
        else if (a <= 0xbfff)
        {
            if (Mbc.CartRamOn)
                return Sram[a - 0xa000 + (0x2000 * IO.Mmu.Mbc.RamBank)];
            return 0xff;
        }
        else if (a <= 0xcfff)
            return Ram[a];
        else if (a <= 0xdfff)
        {
            var wa = a - 0xd000 + (0x1000 * (IO.SVBK == 0 ? 1 : IO.SVBK));
            Watchpoints(wa, Wram[wa], BPType.Read);
            return Wram[wa];
        }
        else if (a <= 0xfeff || a >= 0xff80)
            return Ram[a];
        else if (a >= 0xff00 && a <= 0xff7f || a == 0xfffe || a == 0xffff)
            return IO.Read(a);

        return 0xff;
    }

    public void Write(int a, byte v, bool savestate = false)
    {
        a &= 0xffff;
        if (a <= 0x3fff)
            Mbc.WriteRom0(a, v);
        else if (a <= 0x7fff)
            Mbc.WriteRom1(a, v);
        else if (a <= 0x7fff)
        { }
        else if (a <= 0x9fff)
        {
            Vram[a - 0x8000 + 0x2000 * IO.VBK] = v;
            Ram[a] = v;
        }
        else if (a <= 0xbfff)
        {
            if (Mbc.CartRamOn)
                Sram[a - 0xa000 + (0x2000 * IO.Mmu.Mbc.RamBank)] = v;
        }
        else if (a <= 0xcfff)
        {
            Wram[a - 0xc000] = v;
            Ram[a] = v;
        }
        else if (a <= 0xdfff)
        {
            Wram[a - 0xd000 + (0x1000 * (IO.SVBK == 0 ? 1 : IO.SVBK))] = v;
            Ram[a] = v;
        }
        else if (a <= 0xfeff)
            Ram[a] = v;
        else if (a <= 0xffff)
        {
            if (a <= 0xff7f)
                IO.Write(a, v);
            else
                Ram[a] = v;
        }

        if (!savestate)
            Watchpoints(a, v, BPType.Write);

        //ProcessCheats(a, ref v, BPType.Write);
    }

    public byte ReadDirect(int a) => Ram[(ushort)a];
    public void WriteDirect(int a, byte v) => Ram[(ushort)a] = v;

    public void WriteBlock(int src, int dst, int size)
    {
        Span<byte> srcbytes = null;
        Span<byte> dstbytes;
        if (src <= 0x7fff)
            srcbytes = Mbc.ReadRomBlock(src, size);
        else if (src >= 0xa000 && src <= 0xbfff)
            srcbytes = new Span<byte>(Sram, src, size);
        else if (src >= 0xd000 && src <= 0xdfff)
            srcbytes = new Span<byte>(Wram, src - 0xd000 + (IO.SVBK * 0x1000), size);
        else
            srcbytes = new Span<byte>(Ram, src, size);

        dstbytes = new Span<byte>(Vram, (dst - 0x8000 + IO.VBK * 0x2000) & 0x3fff, size);
        srcbytes.CopyTo(dstbytes);

    }

    public void WriteVramBanks(int a, byte v) => Vram[(ushort)a] = v;

    public byte ReadDMA(int a, bool savestate = false)
    {
        a &= 0xffff;
        if (a <= 0x3fff)
            return Mbc.ReadRom0(a);
        else if (a <= 0x7fff)
            return Mbc.ReadRom1(a);
        else if (a <= 0x9fff)
            return Vram[a - 0x8000 + (0x2000 * IO.VBK)];
        else if (a >= 0xd000 && a <= 0xdfff)
            return Wram[a - 0xd000 + (0x1000 * IO.SVBK)];
        else
            return Ram[a];
    }

    public void WriteDMA(int a, byte v, bool savestate = false)
    {
        a &= 0xffff;
        if (a >= 0x8000 && a <= 0x9fff)
        {
            Vram[a - 0x8000 + 0x2000 * IO.VBK] = v;
            Ram[a] = v;
        }
        else if (a >= 0xd000 && a <= 0xdfff)
        {
            Wram[a - 0xd000 + (IO.SVBK * 0x1000)] = v;
            Ram[a] = v;
        }
        else if (a >= 0xfe00 && a <= 0xfe9f)
            Ram[a] = v;
    }

    public ushort ReadWord(int a) => (ushort)(Ram[a] | Ram[a + 1] << 8);
    public MBC LoadRom(Cartridge Cart, string romname)
    {
        if (RomLoaded)
        {
            Cart.Save(Sram);
            CheatsWindow.Save(Cart.Name);
        }

        if ((Rom = Cart.LoadFile(romname)) == null)
            return null;

        Mbc = Cart.Type switch
        {
            0x00 => new MBC0(),
            >= 1 and <= 0x03 => new MBC1(),
            0x05 or 0x06 => new Mbc2(),
            >= 0x0f and <= 0x13 => new Mbc3(),
            >= 0x19 and <= 0x1e => new Mbc5(),
            _ => new MBC0(),
        };

        Mbc.Init(new byte[0x4000], Cart.CGB);

        if (Mbc != null)
        {
            if (Cart.IsBios)
            {
                //for (int i = 0x100; i < 0x8000; i++)
                //    Rom = Rom.Append<byte>(0).ToArray();
                //Array.Copy(cart.LogoImage, 0x0000, Rom, 0x0104, cart.LogoImage.Length);
                return null;
            }
            Mbc.Init(Rom, Cart.CGB);
            RomName = romname;

            var sram = Sram;
            Cart.Load(ref sram);
            CheatsWindow.Load(Cart.Name);
            RomLoaded = true;
        }
        else
            RomLoaded = false;

        return Mbc;
    }

    public void Reset(bool cgb)
    {
        Mbc.Init(Rom, cgb);
    }

    private void ProcessCheats(int addr, ref byte v)
    {
        if (CheatsWindow.Cheats.Count == 0)// || Main.FastForward)
            return;
        foreach (var cht in CheatsWindow.Cheats)
        {
            if (cht.Enabled && addr == cht.Address && cht.Compare == v)
            {
                if (addr <= 0x7fff)
                {
                    v = cht.Value;
                    return;
                }
                else if (addr <= 0xbfff)
                    Sram[cht.Address & 0xfff] = cht.Value;
                else if (addr <= 0xcfff)
                    Ram[cht.Address] = cht.Value;
                else if (addr <= 0xdfff)
                    Wram[(cht.Address & 0xfff) + cht.Bank * 0x1000] = cht.Value;
            }
        }
    }

    public void Watchpoints(int a, byte v, int type, int ramtype = 0)
    {
        foreach (Breakpoint bp in Breakpoint.BPs)
        {
            var wramaddr = a - 0xd000 + (IO.SVBK * 0x1000);
            if ((bp.Type & BPType.Read) == type)
            {
                if (bp.Enabled && a == bp.Addr && bp.Access(a, v))
                    Cpu.State = Debugging;
                else if (wramaddr == bp.Addr && bp.Access(wramaddr, v))
                    Cpu.State = Debugging;
            }
            else if ((bp.Type & BPType.Write) == type)
            {
                if (bp.Enabled)
                {
                    if (a == bp.Addr && bp.Access(a, v))
                        Cpu.State = Debugging;
                    else if (wramaddr == bp.Addr && bp.Access(wramaddr, v))
                        Cpu.State = Debugging;
                }
            }
        }
    }

    public override List<byte> Save()
    {
        return
        [
            ..new Span<byte>(Vram, 0x0000, Vram.Length).ToArray(),
            ..new Span<byte>(Sram, 0x0000, Sram.Length).ToArray(),
            ..new Span<byte>(Wram, 0x0000, Wram.Length).ToArray(),
            ..new Span<byte>(Ram, 0x0000, Ram.Length).ToArray(),
        ];
    }

    public override void Load(BinaryReader br)
    {
        br.ReadBytes(Vram.Length).CopyTo(Vram, 0x0000);
        br.ReadBytes(Sram.Length).CopyTo(Sram, 0x0000);
        br.ReadBytes(Wram.Length).CopyTo(Wram, 0x0000);
        br.ReadBytes(Ram.Length).CopyTo(Ram, 0x0000);
    }
}

public record Cheat
{
    public string Description { get; set; }
    public int Address { get; set; }
    public byte Compare { get; set; }
    public byte Value { get; set; }
    public int Type { get; set; }
    public int Bank { get; set; }
    public bool Enabled { get; set; }
    public string Codes { get; set; }

    public Cheat(string description, int address, byte compare, byte value, int type, bool enabled, string codes)
    {
        Description = description;
        Address = address;
        Compare = compare;
        Value = value;
        Type = type;
        Enabled = enabled;
        Codes = codes;
        if (type == GameShark)
            Bank = compare;
    }
    public Cheat() { }
}

public record RawCode
{
    public int Address { get; set; }
    public byte Compare { get; set; }
    public byte Value { get; set; }
    public int Type { get; set; }

    public RawCode(int address, byte compare, byte value, int type)
    {
        Address = address;
        Compare = compare;
        Value = value;
        Type = type;
    }
}
