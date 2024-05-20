using ImGuiNET;

namespace GBoy.Gui
{
    public static class IOWindow
    {
        public static void Render(IO IO)
        {
            if (ImGui.BeginTabBar("##IORegs"))
            {
                if (ImGui.BeginTabItem("LCD"))
                {
                    ImGui.BeginTable("IO3", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("");
                    ImGui.TableSetupColumn("");
                    ImGui.TableHeadersRow();
                    TableRow("Cycle", $"{IO.Ppu.Dots}");
                    TableRow("Scanline", $"{IO.LY}");
                    TableRow("DMA Source", $"{IO.HDMA1:X2}{IO.HDMA2:X2}");
                    TableRow("DMA Destination", $"{IO.HDMA3:X2}{IO.HDMA4:X2}");
                    TableRow("DMA Size", $"{IO.HDMA5:X2}");
                    ImGui.EndTable();

                    ImGui.BeginTable("IO0", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("FF40");
                    ImGui.TableSetupColumn("LCDC");
                    ImGui.TableHeadersRow();
                    foreach (var e in IO.GetLCDC())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.BeginTable("IO1", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("FF41");
                    ImGui.TableSetupColumn("STAT");
                    ImGui.TableHeadersRow();
                    foreach (var e in IO.GetSTAT())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.BeginTable("IO2", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("Display");
                    ImGui.TableSetupColumn("");
                    ImGui.TableHeadersRow();
                    TableRow("SCY", $"{IO.SCY:X2}");
                    TableRow("SCX", $"{IO.SCX:X2}");
                    TableRow("LY", $"{IO.LY:X2}");
                    TableRow("LYC", $"{IO.LYC:X2}");
                    TableRow("BGP", $"{IO.BGP:X2}");
                    TableRow("OBP0", $"{IO.OBP0:X2}");
                    TableRow("OBP1", $"{IO.OBP1:X2}");
                    TableRow("WY", $"{IO.WY:X2}");
                    TableRow("WX", $"{IO.WX:X2}");
                    TableRow("BGPI", $"{IO.BGPI:X2}");
                    TableRow("BGPD", $"{IO.BGPD:X2}"); ;
                    TableRow("OBPI", $"{IO.OBPI:X2}");
                    TableRow("OBPD", $"{IO.OBPD:X2}"); ;
                    ImGui.EndTable(); ;

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("CH1"))
                {
                    ImGui.BeginTable("IO7", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("Ff10-FF14");
                    ImGui.TableSetupColumn("Channel 1");
                    ImGui.TableHeadersRow();
                    foreach (var e in IO.GetChannel1())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("CH2"))
                {
                    ImGui.BeginTable("IO8", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("Ff16-FF19");
                    ImGui.TableSetupColumn("Channel 2");
                    ImGui.TableHeadersRow();
                    foreach (var e in IO.GetChannel2())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("CH3"))
                {
                    ImGui.BeginTable("IO9", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("Ff1A-FF1E");
                    ImGui.TableSetupColumn("Channel 3");
                    ImGui.TableHeadersRow();
                    foreach (var e in IO.GetChannel3())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("CH4"))
                {
                    ImGui.BeginTable("IO10", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("Ff20-FF23");
                    ImGui.TableSetupColumn("Channel 4");
                    ImGui.TableHeadersRow();
                    foreach (var e in IO.GetChannel4())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("IRQ"))
                {
                    ImGui.BeginTable("IO5", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("FF0F");
                    ImGui.TableSetupColumn("IF");
                    ImGui.TableHeadersRow();
                    TableRow("DIV", $"{IO.DIV:X4}");
                    TableRow("TIMA", $"{IO.TIMA:X2}");
                    TableRow("TMA", $"{IO.TMA:X2}");
                    TableRow("TAC", $"{IO.TAC:X2}");
                    ImGui.EndTable();

                    ImGui.BeginTable("IO5", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("FF0F");
                    ImGui.TableSetupColumn("IF");
                    ImGui.TableHeadersRow();
                    TableRow("IF", $"{IO.IF:X2}");
                    foreach (var e in IO.GetIF())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.BeginTable("IO6", 2, ImGuiTableFlags.Borders);
                    ImGui.TableSetupColumn("FFFF");
                    ImGui.TableSetupColumn("IE");
                    ImGui.TableHeadersRow();
                    TableRow("IE", $"{IO.IE:X2}");
                    foreach (var e in IO.GetIE())
                        TableRow(e.Key, $"{e.Value}");
                    ImGui.EndTable();

                    ImGui.EndTabItem();
                }
            }
        }
    }
}
