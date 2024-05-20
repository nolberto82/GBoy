using GBoy.Core.MBCs;
using GBoy.Gui;
using System.Text;

namespace GBoy.Core;
public abstract class SaveState
{
    public abstract List<byte> Save();
    public abstract void Load(BinaryReader br);
    private const string SaveStateVersion = "1.03";

    public static void Save(Gui.Program Main)
    {
        if (!Main.Mmu.RomLoaded) return;
        List<byte> data =
        [
            ..Encoding.ASCII.GetBytes(SaveStateVersion),
            ..Main.Mmu.Save(),
            ..Main.Cpu.Save(),
            ..Main.Ppu.Save(),
            ..Main.Apu.Save(),
            ..Main.Apu.Square1.Save(),
            ..Main.Apu.Square2.Save(),
            ..Main.Apu.Wave.Save(),
            ..Main.Apu.Noise.Save(),
            ..Main.IO.Save(),
            ..Main.Mbc.RomBank.GetBytes()
        ];

        var name = $"{Environment.CurrentDirectory}\\{StateDirectory}\\{Path.GetFileNameWithoutExtension(Main.Mmu.RomName)}.sav";
        File.WriteAllBytes(name, data.ToArray());
        Notifications.Init("State Saved Successfully");
    }

    public static void Load(Program Main)
    {
        if (!Main.Mmu.RomLoaded) return;
        var name = $"{Environment.CurrentDirectory}\\{StateDirectory}\\{Path.GetFileNameWithoutExtension(Main.Mmu.RomName)}.sav";
        if (!File.Exists(name)) return;
        using (BinaryReader br = new(new FileStream(name, FileMode.Open, FileAccess.Read)))
        {
            var version = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (version == SaveStateVersion)
            {
                Main.Mmu.Load(br);
                Main.Cpu.Load(br);
                Main.Ppu.Load(br);
                Main.Apu.Load(br);
                Main.Apu.Square1.Load(br);
                Main.Apu.Square2.Load(br);
                Main.Apu.Wave.Load(br);
                Main.Apu.Noise.Load(br);
                Main.IO.Load(br);
                Main.Mbc.RomBank = br.ReadInt32();
                Notifications.Init("State Loaded Successfully");
            }
            else
                Notifications.Init("Error Loading Save State");
        }
    }
}
