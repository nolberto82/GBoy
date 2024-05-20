using GBoy.Gui;
using static GBoy.Core.Cpu;

namespace GBoy.Core;
public class Logger
{
    public StreamWriter Outfile { get; private set; }
    public bool Logging { get; private set; }
    public Mmu Mmu { get; private set; }
    public Cpu Cpu { get; private set; }

    public Logger(Mmu mmu, Cpu cpu)
    {
        Mmu = mmu;
        Cpu = cpu;
    }

    public void LogToFile(ushort pc)
    {
        if (Outfile != null && Outfile.BaseStream.CanWrite)
        {
            bool gamedoctor = false;
            DisasmEntry e = Disassemble(pc, true, gamedoctor);
            if (gamedoctor)
                Outfile.WriteLine($"{e.regtext}");
            else
                Outfile.WriteLine($"{e.pc:X4}  {e.dtext.ToUpper(),-26} {e.regtext}");
        }
    }

    public void OpenCloseLog(bool log = true)
    {
        if (!log)
            Logging = false;
        else
            Logging = !Logging;

        if (Logging)
            Outfile = new StreamWriter(Environment.CurrentDirectory + "\\trace.log");
        else
        {
            if (Outfile != null)
                Outfile.Close();
        }
    }

    public void Reset() => OpenCloseLog(false);
    public DisasmEntry Disassemble(ushort pc, bool get_registers, bool gamedoctor)
    {
        string data = string.Empty;
        string bytes = string.Empty;
        string regtext = string.Empty;
        byte b1 = Mmu.Read(pc + 1, true);
        byte b2 = Mmu.Read(pc + 2, true);
        byte b3 = Mmu.Read(pc + 3, true);

        Opcode d;

        byte op = Mmu.Read(pc, true);
        if (op == 0xcb)
            d = Cpu.opInfoCB[Mmu.Read(pc + 1)];
        else
            d = Cpu.opInfo00[op];

        if (d.Size == 1)
        {
            if (d.Oper.Contains("n/a"))
                data = $"{d.Name}";
            else
            {
                if (d.Oper.Contains(","))
                    data = $"{d.Name} {d.Oper.Insert(d.Oper.IndexOf(",") + 1, " ")}";
                else
                    data = $"{d.Name} {d.Oper}";
            }
            bytes = $"{op:X2}";
        }
        else if (d.Size == 2)
        {
            if (d.Name.Contains("j"))
            {
                ushort offset = (ushort)(pc + (sbyte)b1 + 2);
                if (d.Oper != string.Empty)
                    data = $"{d.Name} {d.Oper} ${offset.ToString(d.Format)}";
                else
                    data = $"{d.Name} ${offset.ToString(d.Format)}";
            }
            else if (d.Name == "ldh")
            {
                if (op == 0xe0)
                    data = $"{d.Name} (${(0xff00 + b1):x4}), a";
                else
                    data = $"{d.Name} a, (${(0xff00 + b1):x4})";
            }
            else if (op == 0xe2)
                data = $"{d.Name} ($ff00+c), a";
            else
            {
                if (d.Oper != string.Empty)
                {
                    if (op == 0xf8)
                        data = $"{d.Name} {d.Oper.Insert(3, " ")}${b1:x2}";
                    else
                        data = $"{d.Name} {d.Oper} ${b1.ToString(d.Format)}";
                }
                else
                    data = $"{d.Name} ${b1.ToString(d.Format)}";
            }

            bytes = $"{op:X2} {b1:X2}";
        }

        else if (d.Size == 3)
        {
            if (d.Oper != string.Empty)
            {
                if (op == 0xea)
                    data = $"{d.Name} (${(b2 << 8 | b1).ToString(d.Format)}), {d.Oper}";
                else if (op == 0xfa)
                    data = $"{d.Name} a, (${(b2 << 8 | b1).ToString(d.Format)})";
                else
                    data = $"{d.Name} {d.Oper} ${(b2 << 8 | b1).ToString(d.Format)}";
            }
            else
                data = $"{d.Name} ${(b2 << 8 | b1).ToString(d.Format)}";
            bytes = $"{op:X2} {b1:X2} {b2:X2}";
        }
        else if (d.Size == 4)
        {
            if (d.Oper.Contains("u16"))
            {
                d.Oper = d.Oper.Replace("u16", $"${b3:X2}{b2:X2}");
                data = $"{d.Name,-4} {d.Oper}";
            }
            else if (d.Oper.Contains(",d8"))
            {
                d.Oper = d.Oper.Replace(",d8", $",${b3:X2}");
                d.Oper = d.Oper.Replace("u8", $"${b2:X2}");
                data = $"{d.Name,-4} {d.Oper}";
            }
            bytes = $"{op:X2} {b1:X2} {b2:X2} {b3:X2} ";
        }

        if (d.Name.Contains("pre"))
        {
            d.Size = 1;
            data = $"pre{"",-5}";
            bytes = $"{op:X2}";
        }

        if (get_registers)
        {
            if (gamedoctor)
            {
                bytes = $"{op:X2},{b1:X2},{b2:X2},{b3:X2}";
                regtext = $"A:{Cpu.A:X2} F:{Cpu.F:X2} B:{Cpu.B:X2} C:{Cpu.C:X2} " +
                          $"D:{Cpu.D:X2} E:{Cpu.E:X2} H:{Cpu.H:X2} L:{Cpu.L:X2} " +
                          $"SP:{Cpu.SP:X4} PC:{pc:X4} " +
                          $"PCMEM:{bytes}";
            }
            else
            {
                bytes = $"{op:X2},{b1:X2},{b2:X2},{b3:X2}";
                char[] flags =
                    [
                        Cpu.F.GetBit(7) ? 'Z' : 'z',
                        Cpu.F.GetBit(6) ? 'N' : 'n',
                        Cpu.F.GetBit(5) ? 'H' : 'h',
                        Cpu.F.GetBit(4) ? 'C' : 'c'
                    ];
                regtext = $"A:{Cpu.A:X2} B:{Cpu.B:X2} C:{Cpu.C:X2} D:{Cpu.D:X2} " +
                          $"E:{Cpu.E:X2} F:{new(flags)} H:{Cpu.H:X2} L:{Cpu.L:X2} " +
                          $"SP:{Cpu.SP:X4}";// Cy:{GbSys.ppu.Cycle + 2}";
                                            //regtext = emu.ppu.Cycle > 0 ? $"Cy:{Program.emu.ppu.Cycle + 2}" : "Cy:0";
            }
        }
        return new(pc, d.Name, d.Oper, "", regtext, data, bytes, d.Size);
    }

    private ushort GetRegValue(string reg)
    {
        var v = reg switch
        {
            "bc" => Cpu.BC,
            "de" => Cpu.DE,
            "hl" => Cpu.HL,
            _ => 0,
        };
        return (ushort)v;
    }
}

public class DisasmEntry
{
    public int pc;
    public string name;
    public string oper;
    public string pctext;
    public string regtext;
    public string dtext;
    public string bytetext;
    public int size;
    public int addr;

    public DisasmEntry(int pc, string name, string oper, string pctext, string regtext, string dtext, string bytetext, int size)
    {
        this.pc = pc;
        this.name = name;
        this.oper = oper;
        this.pctext = pctext;
        this.regtext = regtext;
        this.dtext = dtext;
        this.bytetext = bytetext;
        this.size = size;
    }
}
