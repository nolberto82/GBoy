using GBoy.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GBoy.Gui
{
    internal static class CheatsWindow
    {
        private static string CheatName;
        private static string CheatInput;
        private static string CheatOutput;

        public static List<Cheat> Cheats { get; private set; }

        static CheatsWindow()
        {
            CheatName = CheatInput = CheatOutput = "";
            Cheats = new();
        }

        public static void Render(Mmu Mmu)
        {
            var size = ImGui.GetContentRegionAvail();
            bool opendialog = false;

            ImGui.BeginChild("##cheattable", new(0, -25));
            if (ImGui.BeginTable("##cheatlist", 3, ImGuiTableFlags.RowBg, new(0, -1)))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0);

                for (int i = 0; i < Cheats.Count;)
                {
                    var cht = Cheats.Where(c => c.Description == Cheats[i].Description).ToList();
                    if (cht.Any())
                    {
                        ImGui.PushID(cht[0].Description);
                        var enabled = cht[0].Enabled;
                        TableRow(ref enabled, "", $"{cht[0].Description}");
                        cht[0].Enabled = enabled;
                        ImGui.TableNextColumn();
                        if (ImGui.Button("Edit"))
                        {
                            opendialog = true;
                            CheatName = cht[0].Description;
                            CheatInput = cht[0].Codes;
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("Delete"))
                        {
                            Cheats.RemoveAll(x => x.Description == cht[0].Description);
                            Save(Mmu.RomName);
                        }
                        ImGui.TableNextRow();
                        if (!cht[0].Enabled)
                            Cheats.ForEach(x => { if (x.Description == cht[0].Description) x.Enabled = false; });
                        else
                            Cheats.ForEach(x => { if (x.Description == cht[0].Description) x.Enabled = true; });
                        ImGui.PopID();
                        i += cht.Count;
                    }
                }
                ImGui.EndTable();


            }
            ImGui.EndChild();

            ImGui.BeginGroup();
            if (ImGui.Button("Add Cheats", new(-1, 0)))
                ImGui.OpenPopup("Cheat Input");
            ImGui.EndGroup();

            var open = true;
            if (opendialog)
                ImGui.OpenPopup("Cheat Input");

            ImGui.SetNextWindowSize(new(220, 195));
            if (ImGui.BeginPopupModal("Cheat Input", ref open))
            {
                size = ImGui.GetContentRegionAvail();
                ImGui.Text("Name:"); ImGui.SameLine();
                ImGui.PushItemWidth(-1);
                ImGui.InputText("##cheatname", ref CheatName, 256, ImGuiInputTextFlags.EnterReturnsTrue);
                ImGui.PopItemWidth();
                OpenCopyContext("chtnamecopy", ref CheatName);
                ImGui.InputTextMultiline("##cheatlines", ref CheatInput, 1000, new(size.X / 2, 0), ImGuiInputTextFlags.EnterReturnsTrue);
                OpenCopyContext("chtinputcopy", ref CheatInput);
                ImGui.SameLine();
                ImGui.InputTextMultiline("##cheatconv", ref CheatOutput, 1000, new(size.X / 2, 0), ImGuiInputTextFlags.EnterReturnsTrue);

                CheatOutput = "";
                if (CheatInput.Length > 0)
                    ConvertCodes(CheatName, CheatInput, Mmu, false);

                ImGui.SetCursorPosX(size.X - 100);
                if (ImGui.Button("Ok", new(50, 0)))
                {
                    if (CheatInput.Length > 0)
                        ConvertCodes(CheatName, CheatInput, Mmu, true);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new(50, 0)))
                    ImGui.CloseCurrentPopup();
            }
        }

        public static void ConvertCodes(string name, string cheats, Mmu Mmu, bool add)
        {
            List<string> codes = new();
            List<RawCode> rawcodes = new();
            var input = cheats.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            int cheatype = input[0].Contains("-") ? GameGenie : GameShark;
            for (int i = 0; i < input.Count; i++)
            {
                var c = input[i].ReplaceLineEndings("").Replace("\r", "");
                if (c == "")
                    continue;
                c = c.Replace("-", "").ReplaceLineEndings("");
                if (!add)
                {
                    (int addr, byte cmp, byte val, int type) = DecryptCode(c);
                    if (addr == -1)
                        continue;
                    if (c.Length == 9)
                        CheatOutput += $"{addr:X4}:{cmp:X2}:{val:X2}\n";
                    else if (c.Length == 8)
                        CheatOutput += $"{addr:X4}:{val:X2}\n";
                }
                else
                {
                    (int addr, byte cmp, byte val, int type) = DecryptCode(c);
                    codes.Add($"{input[i]}\r\n");
                    rawcodes.Add(new RawCode(addr, cmp, val, type));
                }
            }
            foreach (var r in rawcodes)
                Cheats.Add(new(CheatName, r.Address, r.Compare, r.Value, r.Type, true, string.Join("", codes.ToArray())));
        }

        public static (int, byte, byte, int) DecryptCode(string c)
        {
            if (c.Length == 9)
            {
                if (c.Replace("\r", "").ToUpper().All("01234567890ABCDEF".Contains))
                {
                    var addr = Convert.ToUInt16($"{c[5]}{c[2]}{c[3]}{c[4]}", 16) ^ 0xf000;
                    byte cmp = (byte)(Convert.ToByte($"{c[6]}{c[8]}", 16).Ror(2) ^ 0xba);
                    var val = Convert.ToByte(c[..2], 16);
                    return (addr, cmp, val, GameGenie);
                }
            }
            else if (c.Length == 8)
            {
                var addr = Convert.ToUInt16($"{c.Substring(6, 2)}{c.Substring(4, 2)}", 16);
                var cmp = Convert.ToByte($"{c.Substring(0, 2)}", 16);
                var val = Convert.ToByte($"{c.Substring(2, 2)}", 16);
                return (addr, cmp, val, GameShark);
            }
            return (-1, 0, 0, -1);
        }

        public static void Load(string name)
        {
            Cheats.Clear();
            var json = @$"{CheatDirectory}/{Path.GetFileNameWithoutExtension(name)}.json";
            var mgbacht = @$"{CheatDirectory}/{Path.GetFileNameWithoutExtension(name)}.cheats";
            if (File.Exists(json))
            {
                var res = JsonSerializer.Deserialize<List<Cheat>>(File.ReadAllText(json));
                foreach (var cht in res)
                {
                    var rawcodes = new List<RawCode>();
                    foreach (var line in cht.Codes.Split("\r\n"))
                    {
                        var c = line.ReplaceLineEndings("").Replace("\r", "").Replace("-", "");
                        if (c == "")
                            continue;
                        (int addr, byte cmp, byte val, int type) = DecryptCode(c);
                        rawcodes.Add(new RawCode(addr, cmp, val, type));
                    }
                }

                if (res != null)
                    Cheats.AddRange(res);
            }
            else
            {
                if (File.Exists(mgbacht))
                {
                    List<string> chtcodes = new();
                    var txt = File.ReadAllText(mgbacht).Split("#");
                    for (int i = 1; i < txt.Length; i++)
                    {
                        Cheat cht = new();
                        var lines = txt[i].Split("\n");
                        for (int j = 0; j < lines.Length; j++)
                        {
                            var line = lines[j];
                            if (line == "" || line[0] == '!')
                            {
                                Cheats.Add(cht);
                                break;
                            }

                            if (line[0] == ' ')
                            {
                                cht.Description = line.TrimStart();
                            }
                            else
                            {
                                cht.Codes += $"{line}\r\n";
                            }
                        }
                    }
                }
            }
        }

        public static void Save(string name)
        {
            if (Cheats.Count == 0) return;
            var chtfile = @$"{CheatDirectory}/{Path.GetFileNameWithoutExtension(name)}.json";
            JsonSerializerOptions options = new() { WriteIndented = true };
            var json = JsonSerializer.Serialize(Cheats, options);
            File.WriteAllText(chtfile, json);
        }
    }
}
