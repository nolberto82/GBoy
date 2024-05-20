using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using GBoy;
using System.Numerics;
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;

namespace GBoy.Gui;
public class GraphicsWindow
{
    private static int MapSelection;
    private static uint[][] TileBuffer;
    private static uint[] SpriteBuffer;
    private static uint[] MapBuffer;
    public static RenderTexture2D Screen { get; private set; }

    public static float MenuHeight { get; private set; }
    public static bool CheatWindow;

    private static Texture2D[] Tiles;
    private static Texture2D Sprites;
    private static Texture2D Map;

    private static Cpu Cpu;
    private static Mmu Mmu;
    private static Ppu Ppu;
    private static IO IO;
    private static Cartridge Cart;
    private static LuaApi LuaApi;

    private static Action ResetEmulator;
    private static Action<string> OpenFile;

    public static bool IsDeck { get; private set; }
    public static bool IsDeckDebug { get; private set; } = false;
    public static bool DebuggerVisible { get; private set; } = true;
    private static bool ShowPpuDebug;
    private static bool ShowApuGraph;

    public static void Init(Program m)
    {
        Cpu = m.Cpu;
        Mmu = m.Mmu;
        Ppu = m.Ppu;
        IO = m.IO;
        Cart = m.Cart;
        LuaApi = m.LuaApi;

        ResetEmulator = m.Reset;
        OpenFile = m.OpenFile;

        IsDeck = Raylib.GetMonitorWidth(0) == 1280 && Raylib.GetMonitorHeight(0) == 800;

        //Screen = Texture.CreateTexture(Ppu.ScreenBuffer, GbWidth, GbHeight);
        Screen = Raylib.LoadRenderTexture(GbWidth, GbHeight);

        SpriteBuffer = new uint[256 * 256 * 4];
        Sprites = Texture.CreateTexture(SpriteBuffer, 256, 256);
        MapBuffer = new uint[256 * 256 * 4];
        TileBuffer = new uint[2][];
        TileBuffer[0] = new uint[128 * 256 * 4];
        TileBuffer[1] = new uint[128 * 256 * 4];

        Map = Texture.CreateTexture(MapBuffer, 256, 256);
        Tiles = new Texture2D[2];
        Tiles[0] = Texture.CreateTexture(TileBuffer[0], 128, 256);
        Tiles[1] = Texture.CreateTexture(TileBuffer[1], 128, 256);



#if DECKDEBUG || DECKRELEASE
        IsDeck = true;
#if DECKDEBUG
        IsDeckDebug = true;
#endif
#endif

#if !DEBUG
        DebuggerVisible = false;
#endif

        if (IsDeck)
        {
            FileBrowser.Opened = true;
            Cpu.State = Paused;
        }
    }

    public static void RenderMenu(ImFontPtr Consolas, Apu Apu)
    {
        ImGui.PushFont(Consolas);
        if (ImGui.BeginMainMenuBar())
        {
            MenuHeight = ImGui.GetWindowHeight();
            if (ImGui.BeginMenu("Browser"))
            {
                if (ImGui.MenuItem("Open Menu", "", CheatWindow))
                {
                    if (!FileBrowser.Opened)
                    {
                        FileBrowser.Open();
                        Cpu.State = Paused;
                    }
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Emulator"))
            {
                if (ImGui.MenuItem("Cheats", "", CheatWindow))
                    CheatWindow = !CheatWindow;

                if (ImGui.MenuItem("Reset"))
                    ResetEmulator();

                var vol = Raylib.GetMasterVolume();
                if (ImGui.SliderFloat("Volume", ref vol, 0, 1))
                    Audio.SetVolume(vol);

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("Show Debugger", "", DebuggerVisible))
                    DebuggerVisible = !DebuggerVisible;

                if (ImGui.MenuItem("Show Ppu Debug", "", ShowPpuDebug))
                    ShowPpuDebug = !ShowPpuDebug;

                if (ImGui.MenuItem("Show Apu Graph", "", ShowApuGraph))
                    ShowApuGraph = !ShowApuGraph;

                ImGui.BeginTable("##channels", 2, ImGuiTableFlags.Borders);
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Enabled");
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                TableRow("Square 1", "##ch1", ref IO.Apu.Square1.Play);
                TableRow("Square 2", "##ch2", ref IO.Apu.Square2.Play);
                TableRow("Wave", "##ch3", ref IO.Apu.Wave.Play);
                TableRow("Noise", "##ch4", ref IO.Apu.Noise.Play);
                ImGui.EndTable();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Rom Info", Cart != null))
            {
                ImGui.BeginTable("##rominfo", 2);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 100);
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                TableRow("Name", Path.GetFileNameWithoutExtension(Cart.Name));
                TableRow("Mapper", $"{MapperTypes[Cart.Type]}");
                TableRow("Mapper Number", $"{Cart.Type:X2}");
                ImGui.EndTable();

                ImGui.EndMenu();
            }

            FileBrowser.Render(out string file, LuaApi);
            if (file != string.Empty)
                OpenFile(file);

            if (!FileBrowser.Opened && Cpu.State == Paused && Cpu.State != Debugging)
                Cpu.State = Running;

            ImGui.EndMainMenuBar();
        }
        ImGui.PopFont();
    }

    public static void RenderScreen(ImFontPtr consolas, bool deck = false)
    {
        var width = Raylib.GetScreenWidth();
        var height = Raylib.GetScreenHeight();
        var texwidth = Screen.Texture.Width;
        var texheight = Screen.Texture.Height;
        var scale = Math.Min((float)width / texwidth, (float)height / texheight);
        Raylib.DrawTexturePro(
            Screen.Texture,
            new Rectangle(0, 0, texwidth, texheight),
            new Rectangle((width - texwidth * scale) / 2,
           ((height - texheight * scale) / 2) + MenuHeight + 3,
            texwidth * scale,
            texheight * scale - MenuHeight),
            Vector2.Zero, 0, Color.White);

        Raylib.DrawFPS(width - 100, (int)(5 + MenuHeight));
        Notifications.Render((int)((width - texwidth * scale) / 2), (int)MenuHeight, width, height, IsDeck);
    }

    public static void RenderScreenDebug(ImFontPtr Consolas)
    {
        var width = Raylib.GetScreenWidth();
        var height = Raylib.GetScreenHeight();
        ImGui.Image((nint)Screen.Texture.Id, ImGui.GetContentRegionAvail());
        Notifications.RenderDebug(Consolas, 0, 0, width, height, IsDeck);
    }

    public static void Render(ImFontPtr Consolas, Logger Logger, DebugWindow DebugWindow, Apu Apu)
    {
        if (IsDeck && !IsDeckDebug)
            Raylib.SetWindowState(ConfigFlags.MaximizedWindow);

        RenderMenu(Consolas, Apu);

        if (DebuggerVisible)
        {
            if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
                ImGui.DockSpaceOverViewport();

            if (ImGui.Begin("Display", NoScrollFlags))
                RenderScreenDebug(Consolas);
            ImGui.End();

            if (ImGui.Begin("Debugger", NoScrollFlags))
                DebugWindow.Render(Logger);
            ImGui.End();

            if (ImGui.Begin("Cpu Info", NoScrollFlags))
                DebugWindow.RenderCpuInfo(Logger);
            ImGui.End();

            if (ImGui.Begin("Breakpoints", NoScrollFlags))
                DebugWindow.RenderBreakpoints();
            ImGui.End();

            if (ImGui.Begin("Memory Viewer", NoScrollFlags))
                DebugWindow.RenderMemory();
            ImGui.End();

            if (ImGui.Begin("Registers"))
                IOWindow.Render(IO);
            ImGui.End();

            if (CheatWindow)
            {
                ImGui.SetNextWindowSize(new(400, 400), ImGuiCond.Once);
                if (ImGui.Begin("Cheat Codes", ref CheatWindow))
                    CheatsWindow.Render(Mmu);
                ImGui.End();
            }

            if (ShowApuGraph)
            {
                ImGui.SetNextWindowSize(new(285, 300), ImGuiCond.Once);
                if (ImGui.Begin("Apu Graph", ref ShowApuGraph, NoScrollFlags))
                {
                    //var samples = Apu.AudioBuffer;
                    //ImGui.PlotLines("lines", ref samples[0], samples.Length, 0, "", -1, 1, ImGui.GetContentRegionAvail());
                    ImGui.End();
                }
            }

            if (ShowPpuDebug)
            {
                if (ImGui.Begin("Ppu Debug", ref ShowPpuDebug, NoScrollFlags))
                    RenderPpuDebug(Cart.CGB, Consolas);
                ImGui.End();
            }
        }
        else
        {
            RenderScreen(Consolas);
        }

        //Notifications.Render(MenuHeight, IsDeck);

        if (Mmu.RomLoaded)
            LuaApi.Update();
    }

    public static void RenderPpuDebug(bool cgb, ImFontPtr consolas)
    {
        if (ImGui.BeginTabBar("##gfx"))
        {
            Array.Clear(MapBuffer);
            Array.Clear(SpriteBuffer);
            Array.Clear(TileBuffer[0]);
            Array.Clear(TileBuffer[1]);

            var list = ImGui.GetWindowDrawList();
            if (ImGui.BeginTabItem("Map"))
            {
                ImGui.BeginGroup();
                {
                    if (Button("0x9800", MapSelection == 0 ? GREEN : WHITE))
                        MapSelection = 0;
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    if (Button("0x9C00", MapSelection == 1 ? GREEN : WHITE))
                        MapSelection = 1;
                    ImGui.PopStyleColor();
                    ImGui.EndGroup();
                }

                ImGui.BeginGroup();
                {
                    DrawMap(0x9800 + (MapSelection * 0x400), cgb);

                    var pos = ImGui.GetCursorScreenPos();
                    var mp = ImGui.GetMousePos() - pos;
                    var region = ImGui.GetContentRegionAvail();
                    Texture.Update(Map, MapBuffer);
                    ImGui.Image((nint)Map.Id, region);
                    var tilepos = DrawGrid(Map, pos, region, mp, new(32, 32), list);
                    if (tilepos.X >= 0 || tilepos.Y >= 0)
                    {
                        if (ImGui.BeginItemTooltip())
                        {
                            ImGui.BeginTable("##tileinfo", 2);

                            var tilemap = MapSelection == 0 ? Ppu.MapAddr - 0x8000 : 0x1c00;
                            var tilemapaddr = (ushort)(tilemap + tilepos.Y * 32 + tilepos.X);
                            var att = Mmu.Vram[tilemapaddr + 0x2000];
                            TableRow("XY", $"{tilepos.X},{tilepos.Y}");
                            TableRow($"Map Address", $"{tilemapaddr:X4}");
                            TableRow($"Att Address", $"{tilemapaddr + 0x2000:X4}");
                            TableRow($"Tile Address", $"{Mmu.Vram[tilemapaddr] * 16 + (att.GetBit(3) ? 0x2000 : 0):X4}");
                            TableRow($"X Flipped", $"{att.GetBit(5)}");
                            TableRow($"Y Flipped", $"{att.GetBit(6)}");
                            TableRow($"Priority", $"{att.GetBit(7)}");

                            ImGui.EndTable();
                            ImGui.EndTooltip();
                        }
                    }
                    ImGui.EndGroup();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Tiles"))
            {
                var region = new Vector2(16 * 8 * 2, 16 * 8 * 5f);
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 128 * 2 + 16);

                for (int i = 0; i < 2; i++)
                {
                    var pos = ImGui.GetCursorScreenPos();
                    var mp = ImGui.GetMousePos() - pos;

                    RenderTiles((ushort)(0x0000 + i * 0x1000), i, cgb);

                    Texture.Update(Tiles[i], TileBuffer[i]);
                    ImGui.Image((nint)Tiles[i].Id, region);
                    var tilepos = DrawGrid(Tiles[i], pos, region, mp, new(16, 16), list);
                    if (tilepos.X >= 0 || tilepos.Y >= 0)
                    {
                        if (ImGui.BeginItemTooltip())
                        {
                            if (ImGui.BeginTable("##tileinfo", 2))
                            {
                                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed); ImGui.TableSetupColumn("");
                                var id = (int)(tilepos.X + tilepos.Y * 16);
                                var tileaddr = id * 16;
                                TableRow("Tile Addr", $"{tileaddr:X4}");
                                TableRow("Tile Id", $"{id:X2}");

                                ImGui.EndTable();
                            }
                            ImGui.EndTooltip();
                        }
                    }
                    //pos.X += 16 * 8 * 2 + 5;
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Sprites"))
            {
                var pos = ImGui.GetCursorScreenPos();
                var mp = ImGui.GetMousePos() - pos;

                DrawSprite(cgb);

                var region = new Vector2(8 * 6 * 32, 8 * 5 * 32);
                Texture.Update(Sprites, SpriteBuffer);
                ImGui.Image((nint)Sprites.Id, region);
                var tilepos = DrawGrid(Sprites, pos, region, mp, new(8, 5), list);
                int i = (int)(tilepos.X + tilepos.Y * 8);
                if (tilepos.X >= 0 || tilepos.Y >= 0)
                {
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.BeginTable("##spriteinfo", 2);
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed); ImGui.TableSetupColumn("");

                        int sy = Mmu.Ram[0xfe00 + i * 4];
                        int sx = Mmu.Ram[0xfe01 + i * 4];
                        byte ti = Mmu.Ram[0xfe02 + i * 4];
                        byte at = Mmu.Ram[0xfe03 + i * 4];
                        var bank = ((at >> 3) & 1) * 0x2000;
                        var tileaddr = !cgb ? 0x8000 + ti * 16 : bank + (ti & 0xfe) * 16;
                        TableRow("ID", $"{i}");
                        TableRow("XY", $"X: {sx},Y: {sy}");
                        TableRow("Tile Addr", $"{tileaddr:X4}");
                        TableRow("Prority", $"{at.GetBit(7)}");

                        ImGui.EndTable();
                        ImGui.EndTooltip();
                    }
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Palettes"))
            {
                ImGui.BeginChild("##palettes");
                ImGui.Columns(3);
                ImGui.SetColumnWidth(0, 160);
                ImGui.Text("BGP "); ImGui.SameLine();
                for (int i = 0; i < 4; i++)
                {
                    ImGui.ColorButton("", Ppu.GbColors[1][(byte)(IO.BGP >> (i << 1) & 3)].Color4());
                    if (i < 3)
                        ImGui.SameLine();
                }

                ImGui.Text("OBP0"); ImGui.SameLine();
                for (int i = 0; i < 4; i++)
                {
                    ImGui.ColorButton("", Ppu.GbColors[1][(byte)(IO.OBP0 >> (i << 1) & 3)].Color4());
                    if (i < 3)
                        ImGui.SameLine();
                }

                ImGui.Text("OBP1"); ImGui.SameLine();
                for (int i = 0; i < 4; i++)
                {
                    ImGui.ColorButton("", Ppu.GbColors[1][(byte)(IO.OBP1 >> (i << 1) & 3)].Color4());
                    if (i < 3)
                        ImGui.SameLine();
                }

                ImGui.NextColumn();

                ImGui.Text("  CGB Background");
                for (int i = 0; i < Ppu.CGBBkgPal.Length; i += 2)
                {
                    var rgb555 = Ppu.CGBBkgPal[i] | Ppu.CGBBkgPal[i + 1] << 8;
                    var c = Ppu.GetRGB555((ushort)rgb555);
                    if (i / 2 % 4 == 0)
                        ImGui.Text($"{i / 8}"); ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.ColorButton("", c.Color4());
                    ImGui.Text($"{rgb555:X4}");
                    ImGui.EndGroup();
                    if ((i / 2 + 1) % 4 > 0)
                        ImGui.SameLine();
                }

                ImGui.NextColumn();

                ImGui.Text("  CGB Sprites");
                for (int i = 0; i < Ppu.CGBObjPal.Length; i += 2)
                {
                    var rgb555 = Ppu.CGBObjPal[i] | Ppu.CGBObjPal[i + 1] << 8;
                    var c = Ppu.GetRGB555((ushort)rgb555);
                    if (i / 2 % 4 == 0)
                        ImGui.Text($"{i / 8}"); ImGui.SameLine();
                    ImGui.BeginGroup();
                    ImGui.ColorButton("", c.Color4());
                    ImGui.Text($"{rgb555:X4}");
                    ImGui.EndGroup();
                    if ((i / 2 + 1) % 4 > 0)
                        ImGui.SameLine();
                }
                ImGui.Columns(1);
                ImGui.EndChild();

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    public static void DrawMap(int tilemap, bool cgb)
    {
        int bgPixel;

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                int sx, sy;

                sy = y & 255;
                sx = x & 255;

                if (!cgb)
                {
                    int bgaddr = Ppu.GetBgAddr(Ppu.TileAddr, tilemap, sx, sy);
                    byte color = Ppu.GetColor(bgaddr, sx, false);
                    if (sy < 0 || sy > 255) continue;
                    bgPixel = (byte)(IO.BGP >> (color << 1) & 3);
                    MapBuffer[y * 256 + x] = Ppu.GbColors[1][bgPixel] | 0xff000000;
                }
                else
                {
                    if ((ushort)(tilemap - 0x8000 + (sx / 8) + sy / 8 * 32) == 0x1a21)
                    { }
                    var att = Mmu.Vram[(ushort)(tilemap - 0x8000 + (sx / 8) + sy / 8 * 32) + 0x2000];
                    var bank = ((att >> 3) & 1) * 0x2000;
                    int bgaddr = Ppu.GetBgAddr(Ppu.TileAddr, tilemap, sx, sy, att.GetBit(6)) + bank;
                    byte color = Ppu.GetColor(bgaddr, sx, att.GetBit(5));

                    var n = (((att & 7) << 2) + color) << 1;
                    var rgb = Ppu.GetRGB555((ushort)(Ppu.CGBBkgPal[n] | Ppu.CGBBkgPal[n + 1] << 8));
                    MapBuffer[sy * 256 + x] = rgb;
                }
            }
        }
    }

    private static void RenderTiles(ushort tileaddr, int i, bool cgb)
    {
        int tilenum = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int yy = 0; yy < 8; yy++)
                {
                    ushort bgaddr = (ushort)(tileaddr + tilenum * 16 + (yy & 7) * 2);
                    for (int xx = 0; xx < 8; xx++)
                    {
                        int xp = x * 8 + xx;
                        int yp = y * 8 + yy;
                        var pos = yp * 128 + xp;

                        if (!cgb)
                        {
                            int color = Mmu.Ram[bgaddr + 0x8000] >> (7 - xx) & 1 |
                            (Mmu.Ram[bgaddr + 0x8000 + 1] >> (7 - xx) & 1) * 2;
                            TileBuffer[i][pos] = Ppu.GbColors[1][color];
                        }
                        else
                        {
                            var att = Mmu.Vram[(ushort)((x / 8) + y / 8 * 32) + 0x2000];
                            var bank = ((att >> 3) & 1) * 0x2000;
                            int color = Mmu.Vram[bgaddr + bank] >> (7 - xx) & 1 |
                            (Mmu.Vram[bgaddr + bank + 1] >> (7 - xx) & 1) * 2;
                            var n = (((att & 7) << 2) + color) << 1;
                            var rgb = Ppu.GetRGB555((ushort)(Ppu.CGBBkgPal[n] | Ppu.CGBBkgPal[n + 1] << 8));
                            TileBuffer[i][pos] = Ppu.GbColors[1][color];
                        }
                    }
                }
                tilenum++;
            }
        }
    }

    public static void DrawSprite(bool CGB)
    {
        var x = 0;
        var y = 0;
        for (int i = 0; i < 40; i++)
        {
            byte ti = IO.Mmu.Ram[0xfe02 + i * 4];
            byte at = IO.Mmu.Ram[0xfe03 + i * 4];
            bool flipX = at.GetBit(5);
            bool flipY = at.GetBit(6);

            int size = IO.LCDC.GetBit(2) ? 16 : 8;
            int tile = size == 16 ? ti & 0xfe : ti | 1;

            int bgaddr;

            if (i == 6)
            { }

            for (int yy = 0; yy < 8; yy++)
            {
                var fy = flipY ? 7 - yy - size - 1 : yy & 7;
                if (!CGB)
                    bgaddr = 0x8000 + tile * 16 + fy * 2;
                else
                    bgaddr = (at.GetBit(3) ? 0x2000 : 0) + tile * 16 + fy * 2;
                for (int xx = 0; xx < 8; xx++)
                {
                    int pos = yy * 256 + y + xx + x;

                    if (flipY)
                        fy ^= 7;

                    int fx = 7 - xx;
                    if (flipX)
                        fx ^= 7;

                    if (!CGB)
                    {
                        int color = IO.Mmu.Ram[bgaddr] >> fx & 1 |
                        (IO.Mmu.Ram[bgaddr + 1] >> fx & 1) * 2;

                        byte p = at.GetBit(4) ? IO.OBP1 : IO.OBP0;
                        var spPixel = p >> (color << 1) & 3;
                        SpriteBuffer[pos] = Ppu.GbColors[1][spPixel];

                    }
                    else
                    {
                        fx = flipX ? xx ^ 7 : xx;
                        var c = Ppu.GetColor(bgaddr, fx, false);
                        var n = (((at & 7) << 2) + c) << 1;
                        var pal = Ppu.CGBObjPal[n] | Ppu.CGBObjPal[n + 1] << 8;
                        uint rgb = (uint)Ppu.GetRGB555((ushort)pal);
                        SpriteBuffer[pos] = rgb;
                    }
                }
            }

            x += 8;
            if (((i + 1) % 8) == 0)
            {
                x = 0;
                y += 2048;
            }
        }
    }

    public static void RenderText(string[] text, int x, int y, int width, int height, Color c, bool deck = false)
    {
        var scale = Math.Min((float)width / GbWidth, (float)height / GbHeight);
        var fontsize = DebuggerVisible ? 15 : 30;
        if (deck)
            y -= 25;
        Raylib.DrawRectangle(x, y, (int)(GbWidth * scale) + 2, fontsize * text.Length, new(0, 0, 0, 192));
        foreach (var item in text)
        {
            Raylib.DrawText(item, x + 5, y, fontsize, c);
            y += fontsize;
        }
    }

    public static void Unload()
    {
        Raylib.UnloadRenderTexture(Screen);
        Raylib.UnloadTexture(Sprites);
        Raylib.UnloadTexture(Map);
        Raylib.UnloadTexture(Tiles[0]);
        Raylib.UnloadTexture(Tiles[1]);
    }
}
