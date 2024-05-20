namespace GBoy.Core;
public class Cartridge
{
    public string Title { get; private set; } = "";
    public bool CGB { get; private set; }
    public int Type { get; private set; }
    public int RomSize { get; private set; }
    public int RamSize { get; private set; }
    public string Name { get; private set; } = "";
    public bool IsBios { get; private set; }

    public Cartridge() { }

    public byte[] LoadFile(string romname)
    {
        byte[] data = File.ReadAllBytes(romname);
        //header.Title = romname;
        Name = romname;
        CGB = data[0x143].GetBit(7);
        RomSize = 0x8000 * (1 << data[0x148]);
        RamSize = data[0x149];

        if (Path.GetFileName(romname.ToLower()) != "dmg_boot.gb")
        {
            IsBios = false;
            Type = data[0x147];
            if (MapperTypes.ContainsKey(Type))
                Console.WriteLine($"Mapper: {MapperTypes[Type]}");
        }
        else
            IsBios = true;
        return data;
    }

    public void Load(ref byte[] sram)
    {
        if (RamSize > 0)
        {
            var name = $"{Environment.CurrentDirectory}\\{SaveDirectory}\\{Path.GetFileNameWithoutExtension(Name)}.sav";
            if (File.Exists(name))
            {
                var v = File.ReadAllBytes($"{name}");
                v.CopyTo(sram, 0x0000);
            }

        }
    }

    public void Save(Span<byte> sram)
    {
        if (RamSize > 0)
        {
            var name = Path.GetFullPath($"{SaveDirectory}/{Path.GetFileNameWithoutExtension(Name)}.sav");
            if (Directory.Exists(SaveDirectory))
                File.WriteAllBytes($"{name}", sram.ToArray());
        }
    }

    public void Reset(ref byte[] sram)
    {
        Save(sram);
        Load(ref sram);
    }
}
