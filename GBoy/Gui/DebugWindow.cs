

using GBoy.Core.MBCs;
using ImGuiExtra;
using ImGuiNET;
using System.Numerics;
using System.Xml.Linq;

namespace GBoy.Gui;
public class DebugWindow
{
    public const int DisasmMaxLines = 10;
    public const int BreakpointsMaxLines = 10;

    public bool FollowPc { get; private set; }
    public int AsmOffset { get; private set; }
    public int JumpAddr { get; private set; } = -1;

    public Cpu Cpu { get; private set; }
    public Mmu Mmu { get; private set; }
    public MBC Mbc { get; private set; }
    public Cartridge Cart { get; private set; }
    public IO IO { get; private set; }

    public Action Run { get; private set; }
    public Action Reset { get; private set; }
    public Breakpoint Breakpoint { get; private set; }

    public int RamType;
    public const int VramB = 1;
    public const int WramB = 2;

    private MemoryEditor MemoryEditor;

    private string JumpAddress;
    private string BPAddress;
    private string BPCondition;
    public int ScrollOffset;
    public int MinOffset;
    public int MaxOffset;

    public record MemRegion
    {
        public string Name;
        public int Start;
        public int End;
        public int Banks;
        public int Type;
        public int CurrentItem;

        public MemRegion(string name, int start, int end, int banks, int type)
        {
            Name = name;
            Start = start;
            End = end;
            Banks = banks;
            Type = type;
        }
    }
    public List<MemRegion> MemRegions;

    public DebugWindow() { }
    public DebugWindow(Program m)
    {
        Mmu = m.Mmu;
        Mbc = m.Mbc;
        Cpu = m.Cpu;
        IO = m.IO;
        Cart = m.Cart;
        Run = m.ContinueExecution;
        Reset = m.Reset;
        JumpAddress = "";
        BPAddress = "";
        BPCondition = "";

        Breakpoint = m.Breakpoint;

        MemRegions =
        [
            new("Rom0", 0x0000, 0x4000, 8, 0),
            new("Rom1", 0x4000, 0x8000, 8, 1),
            new("Vram", 0x8000, 0xa000, 2, 2),
            new("Sram", 0xa000, 0xc000, 2, 3),
            new("Wram", 0xc000, 0xe000, 2, 4),
            new("Oram", 0xfe00, 0xfea0, 1, 5),
            new("Iram", 0xff00, 0xff80, 1, 6),
            new("Hram", 0xff80, 0x10000, 1, 7),
            new("Vrab", 0x0000, 0x4000, 4, 8),
            new("Wrab", 0x0000, 0x8000, 8, 9),
            new("Srab", 0x0000, 0x8000, 4, 10),
        ];

        MemoryEditor = new(m, MemRegions)
        {
            ReadFn = ReadMem,
            WriteFn = WriteMem,
            OptAddrDigitsCount = 4,
        };
    }

    public void Render(Logger Logger)
    {
        if (ImGui.GetWindowPos().Y < 20)
            ImGui.SetWindowPos(new(ImGui.GetWindowPos().X, 20));

        if (FollowPc)
            AsmOffset = 0;
        var jump = JumpAddr > -1 ? (ushort)JumpAddr : Cpu.PC;
        ushort pc = !FollowPc ? (ushort)(jump + AsmOffset) : jump;

        float mousewheel = ImGui.GetIO().MouseWheel;
        if (mousewheel != 0 && Mmu.RomLoaded)
        {
            if (ImGui.IsWindowHovered())
            {
                FollowPc = false;
                if (mousewheel < 0)
                {
                    AsmOffset += 3;
                    if (AsmOffset + DisasmMaxLines >= Mmu.Rom.Length)
                        AsmOffset -= 3;
                }
                else
                {
                    AsmOffset -= 3;
                    if (AsmOffset + DisasmMaxLines >= Mmu.Rom.Length)
                        AsmOffset += 3;
                }
            }
        }

        for (int i = 0; i < DisasmMaxLines; i++)
        {
            ImGui.PushID(pc);
            DisasmEntry e;
            if (Mmu.RomLoaded)
            {
                byte op = Mmu.Read(pc, true);
                e = Logger.Disassemble(pc, false, false);
            }
            else
                e = new(pc, "", "", "", "", "nop", "00", 1);

            if (e.pc == Cpu.PC)
                if (FollowPc)
                    ImGui.SetScrollHereY(0.25f);

            if (ImGui.Button("x"))
                Breakpoint.InsertRemove(e.pc, BPType.Exec);

            ImGui.SameLine();

            if (ImGui.Selectable($"{pc:X4} {e.bytetext,-9} {e.dtext}", pc == Cpu.PC)) { }

            var found = Breakpoint.BPs.Find(x => x.Addr == pc);
            if (found != null)
            {
                if (found.Enabled && (found.Type & BPType.Exec) != 0)
                    DrawRect(0x4000ff00, 0xff00ff00);
                else
                    DrawRect(0x000000ff, 0xff0000ff);
            }

            if (pc == Cpu.PC)
                DrawRect(0x6000ffff, 0xff00ffff);

            pc += (ushort)e.size;
            if (pc >= Mmu.Ram.Length)
            {
                pc -= DisasmMaxLines;
                break;
            }
        }
    }

    public void RenderCpuInfo(Logger Logger)
    {
        ImGui.Text($"FPS {ImGui.GetIO().Framerate}");
        ImGui.Separator();
        ImGui.BeginGroup();
        {
            ImGui.Text($"AF: {Cpu.AF:X4}");
            ImGui.Text($"BC: {Cpu.BC:X4}");
            ImGui.Text($"DE: {Cpu.DE:X4}"); 
            ImGui.Text($"HL: {Cpu.HL:X4}");
            ImGui.Text($"SP: {Cpu.SP:X4}");
        }
        ImGui.EndGroup();
        ImGui.SameLine();
        ImGui.BeginGroup();
        {
            if (ImGui.Button("Run", ButtonSize) && Mmu.RomLoaded)
            {
                JumpAddr = -1;
                AsmOffset = 0;
                Run();
                FollowPc = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Reset", new(-1,0)))
            {
                MemoryEditor.SelectedRam = 0;
                Reset();
            }

            if (ImGui.Button("Over", ButtonSize) || ImGui.IsKeyPressed(ImGuiKey.F8) && Mmu.RomLoaded)
                StepOver();
            ImGui.SameLine();
            if (ImGui.Button("Into", new(-1, 0)) || ImGui.IsKeyPressed(ImGuiKey.F7) && Mmu.RomLoaded)
                StepInto();

            if (ImGui.Button("1 Line", new(-1, 0)) || ImGui.IsKeyPressed(ImGuiKey.F6))
                StepScanline();

            ImGui.PushStyleColor(ImGuiCol.Button, Logger.Logging ? 0xff00ff00 : 0xff0000ff);
            if (ImGui.Button("Trace", new(-1, 0)))
                Logger.OpenCloseLog();
            ImGui.PopStyleColor();
        }
        ImGui.EndGroup();

        ImGui.BeginGroup();
        {
            Checkbox("C", (Cpu.F & FC) > 0); ImGui.SameLine();
            Checkbox("N", (Cpu.F & FN) > 0); ImGui.SameLine();
            Checkbox("H", (Cpu.F & FH) > 0); ImGui.SameLine();
            Checkbox("Z", (Cpu.F & FZ) > 0);
            Checkbox("IME", Cpu.IME); ImGui.SameLine();
            Checkbox("Halt", Cpu.Halt);
        }
        ImGui.EndGroup();

        if (ImGui.BeginChild("##goto", new(-1, 25), ImGuiChildFlags.FrameStyle))
        {
            if (ImGui.Button("Goto", new Vector2(ButtonSize.X, 0)))
                SetJumpAddress(JumpAddress);

            ImGui.SameLine();
            ImGui.PushItemWidth(-1);
            ImGui.InputText($"##bpinput", ref JumpAddress, 4, HexInputFlags);
            ImGui.PopItemWidth();
            ImGui.EndChild();
        }
    }

    public void RenderBreakpoints()
    {
        var open = true;
        ImGui.SetNextWindowSize(new(100, 155));
        if (ImGui.BeginPopupModal("bpmenu", ref open, NoScrollFlags))
        {
            ImGui.PushItemWidth(-1);
            ImGui.InputText($"##bpinput2", ref BPAddress, 4, HexInputFlags);
            ImGui.PopItemWidth();
            if (ImGui.Button("Add Exec", new(-1, 0)))
            {
                Breakpoint.InsertRemove(BPAddress, BPType.Exec);
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.Button("Add Write", new(-1, 0)))
            {
                Breakpoint.InsertRemove(BPAddress, BPType.Write);
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.Button("Add Read", new(-1, 0)))
            {
                Breakpoint.InsertRemove(BPAddress, BPType.Read);
                ImGui.CloseCurrentPopup();
            }
            ImGui.Separator();
            if (ImGui.Button("Close", new(-1, 0)))
                ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }

        ImGui.BeginGroup();
        {
            ImGui.BeginChild("bplist", new(-1, -20));
            for (int i = 0; i < Breakpoint.BPs.Count; i++)
            {
                var bp = Breakpoint.BPs[i];
                if (ImGui.Button($"{bp.Addr:X4}"))
                    SetJumpAddress(bp.Addr);

                ImGui.SameLine();

                ImGui.PushID(i);

                Checkbox("X", (bp.Type & BPType.Exec) > 0, ref bp, BPType.Exec);
                ImGui.SameLine();
                Checkbox("W", (bp.Type & BPType.Write) > 0, ref bp, BPType.Write);
                ImGui.SameLine();
                Checkbox("R", (bp.Type & BPType.Read) > 0, ref bp, BPType.Read);
                ImGui.SameLine();

                if (ImGui.Button("x"))
                {
                    Breakpoint.BPs.RemoveAt(i);
                    break;
                }
            }
            ImGui.EndChild();
        }

        ImGui.BeginGroup();
        {
            if (ImGui.Button("Add Breakpoint", new(-1, 20)))
                ImGui.OpenPopup("bpmenu");
        }
        ImGui.EndGroup();
    }

    public unsafe void RenderMemory()
    {
        ImGui.BeginTabBar("##mem_tabs", ImGuiTabBarFlags.AutoSelectNewTabs);
        for (int i = 0; i < MemRegions.Count; i++)
        {
            var r = MemRegions[i];
            ImGui.PushStyleColor(ImGuiCol.Text, MemoryEditor.SelectedRam == i ? GREEN : WHITE);
            ImGui.BeginDisabled((r.Name == "Vrab" || r.Name == "Wrab") && !Cart.CGB);
            if (ImGui.BeginTabItem(r.Name.ToUpperInvariant()))
            {
                ImGui.PopStyleColor();
                MemoryEditor.SelectedRam = i;
                ReadOnlySpan<byte> data = new();
                switch (r.Name.ToLowerInvariant())
                {
                    case "rom0":
                    case "rom1":
                        data = new(Mmu.Mbc.Rom, r.Start, r.End - r.Start);
                        break;
                    case "vram":
                    case "sram":
                    case "wram":
                    case "oram":
                    case "iram":
                    case "hram":
                        data = new(Mmu.Ram, r.Start, r.End - r.Start); break;
                    case "vrab": data = Cart.CGB ? new(Mmu.Vram, r.Start, r.End - r.Start) : new(); break;
                    case "wrab": data = Cart.CGB ? new(Mmu.Wram, r.Start, r.End - r.Start) : new(); break;
                    case "srab": data = Cart.CGB ? new(Mmu.Sram, r.Start, r.End - r.Start) : new(Mmu.Sram); break;
                }

                MemoryEditor.DrawContents(data.ToArray(), data.Length, i, r.Start);
                ImGui.EndTabItem();
            }
            ImGui.EndDisabled();
        }
        ImGui.EndTabBar();
    }

    public void StepInto()
    {
        Cpu.Step(Cpu.PC);

        AsmOffset = 0;
        JumpAddr = -1;
        Cpu.State = Stepping;
        FollowPc = true;
    }

    public void StepOver()
    {
        byte op = Mmu.Read(Cpu.PC);
        if (Cpu.opInfo00[op].Name == "call" || Cpu.opInfo00[op].Name == "rst")
        {
            Cpu.StepOverAddr = (ushort)(Cpu.PC + Cpu.opInfo00[op].Size);
            Cpu.State = Running;
            FollowPc = true;
            AsmOffset = 0;
            JumpAddr = -1;
        }
        else
        {
            if (op == 0x76)
            {
                while (op == 0x76)
                {
                    StepInto();
                    op = Mmu.Read(Cpu.PC);
                }
            }
            else
                StepInto();
        }
    }

    public void StepScanline()
    {
        if (!Mmu.RomLoaded) return;

        int oldscanline = IO.LY;

        while (oldscanline == IO.LY)
            StepInto();
    }

    public byte ReadMem(byte[] data, int a)
    {
        if (!Mmu.RomLoaded)
            return 0;
        switch (MemRegions[MemoryEditor.SelectedRam].Name.ToLowerInvariant())
        {
            case "rom0": return Mmu.Mbc.ReadRom0(a + 0x0000);
            case "rom1": return Mmu.Mbc.ReadRom1(a + 0x4000);
            case "vram":
                if (Cart.CGB)
                    return Mmu.Vram[a];
                return Mmu.Ram[a + 0x8000];
            case "sram":
                if (Mmu.Mbc.CartRamOn)
                    return Mmu.Sram[a];
                return 0xff;
            case "wram":
                if (Cart.CGB)
                    return Mmu.Wram[(a & 0xfff) + IO.SVBK * 0x1000];
                return Mmu.Ram[a + 0xc000];
            case "oram": return Mmu.Ram[a + 0xfe00];
            case "iram": return IO.Read(a, true);
            case "hram": return Mmu.Ram[a + 0xff80];
            case "vrab": return Mmu.Vram[a];
            case "wrab": return Mmu.Wram[a];
            case "srab": return Mmu.Sram[a];
            default: return 0;
        }
    }

    public void WriteMem(byte[] data, int a, byte v)
    {
        if (!Mmu.RomLoaded)
            return;
        switch (MemRegions[MemoryEditor.SelectedRam].Name.ToLowerInvariant())
        {
            case "rom0": Mmu.Mbc.WriteRom0(a + 0x0000, v, true); break;
            case "rom1": Mmu.Mbc.WriteRom1(a + 0x4000, v, true); break;
            case "vram":
                Mmu.Ram[a + 0x8000] = v;
                Mmu.Vram[a] = v;
                break;
            case "sram": Mmu.Ram[a + 0xa000] = v; break;
            case "wram":
                Mmu.Ram[a + 0xc000] = v;
                Mmu.Wram[a] = v;
                break;
            case "oram": Mmu.Ram[a + 0xfe00] = v; break;
            case "hram": Mmu.Ram[a + 0xff00] = v; break;
            case "vrab": Mmu.Vram[a] = v; break;
            case "wrab":
                Mmu.Wram[a] = v;
                Mmu.Ram[a + 0xc000] = v;
                break;
            default: Mmu.Ram[a] = v; break;
        }
    }

    public void SetJumpAddress(dynamic addr)
    {
        if (addr.GetType() == typeof(string) && addr == "") return;
        if (addr.GetType() == typeof(string))
            JumpAddr = Convert.ToUInt16(addr, 16);
        else
            JumpAddr = addr;
        AsmOffset = 0;
    }
}
