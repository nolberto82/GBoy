using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace GBoy.Gui;
public class FileBrowser
{
    private static List<FileDetails> GameFiles { get; set; } = new();
    public static string StartDirectory { get; set; } = "C:";
    private static bool opened;
    public static bool Opened { get => opened; set => opened = value; }

    public static int DpadDelay;
    private static int TabIndex;
    public static int[] Selection;
    public static int[] MenuScroll;

    private static readonly string ConfigName = "Directory.txt";

    private static RenderTexture2D MenuRender;

    static FileBrowser()
    {
        MenuScroll = [0, 0];
        Selection = new int[3];
        MenuRender = Raylib.LoadRenderTexture(1280, 800);
    }

    private record FileDetails
    {
        public string Name;
        public bool IsDrive;
        public bool IsFile;

        public FileDetails(string name, bool isDrive, bool isFile)
        {
            Name = name;
            IsDrive = isDrive;
            IsFile = isFile;
        }
    }

    public static void Open(bool button = false)
    {
        Opened = true;
        Enumerate();
    }

    public static void Close() => Opened = false;

    public static void Render(out string filename, LuaApi Lua)
    {
        var io = ImGui.GetIO();
        var isdeck = GraphicsWindow.IsDeck;
        if (isdeck)
            UpdateDeckMenu(TabIndex);

        filename = "";
        if (Opened)
        {
            var vp = ImGui.GetMainViewport();
            ImGui.SetNextWindowSize(vp.Size);
            ImGui.SetNextWindowPos(new(0, GraphicsWindow.MenuHeight));
            if (ImGui.Begin("Menu", ref opened, NoScrollFlags | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoMove))
            {
                if (ImGui.BeginTabBar("##menutabs"))
                {
                    Enumerate("");

                    if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftTrigger1))
                        TabIndex = (TabIndex - 1) < 0 ? 2 : TabIndex - 1;
                    else if (Raylib.IsGamepadButtonPressed(0, GamepadButton.RightTrigger1))
                        TabIndex = (TabIndex + 1) % 3;

                    var open = true;
                    if (ImGui.BeginTabItem("Games", ref open, TabIndex == 0 && isdeck ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                    {
                        var n = 0;
                        if (!isdeck)
                        {
                            ImGui.Text($"{StartDirectory}");
                            ImGui.BeginGroup();
                            for (int i = 0; i < GameFiles.Count; i++)
                            {
                                var file = GameFiles[i];
                                if (file.IsDrive || file.Name == "..")
                                {
                                    n = i;
                                    if (ImGui.Button(file.Name))
                                    {
                                        if (file.Name == "..")
                                        {
                                            GameFiles.Clear();
                                            StartDirectory = Path.GetFullPath(@$"{StartDirectory}/..");
                                            Enumerate();
                                            break;
                                        }
                                        else
                                        {
                                            StartDirectory = file.Name;
                                            Enumerate();
                                            break;
                                        }
                                    }
                                    ImGui.SameLine();
                                }
                            }
                            ImGui.EndGroup();
                        }
                        else
                        {
                            Enumerate(RomDirectory);
                            n = -1;
                        }

                        if (isdeck)
                            ImGui.SetNextWindowScroll(new(0, 0));
                        if (ImGui.BeginChild("##files", new(-1, -45), ImGuiChildFlags.FrameStyle | ImGuiChildFlags.AlwaysAutoResize))
                        {
                            for (int i = n + 1; i < GameFiles.Count; i++)
                            {
                                var file = GameFiles[i];

                                var name = Path.GetFileName(file.Name);
                                if (name == "")
                                    name = file.Name;

                                if (isdeck)
                                {
                                    ImGui.SetWindowFocus();
                                    RenderDeckMenu(0);
                                    rlImGui.ImageRenderTextureFit(MenuRender);
                                    if (Raylib.IsGamepadButtonPressed(0, BtnB))
                                    {
                                        filename = GameFiles[Selection[0]].Name;
                                        Opened = false;
                                    }
                                }
                                else
                                {
                                    if (ImGui.Selectable($"{name}", false, ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.DontClosePopups))
                                    {
                                        if ((ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)))
                                        {
                                            if (File.Exists(file.Name))
                                            {
                                                ImGui.CloseCurrentPopup();
                                                Opened = false;
                                                filename = file.Name;
                                                File.WriteAllText(ConfigName, StartDirectory);
                                            }
                                            else
                                            {
                                                StartDirectory = file.Name;
                                                Enumerate();
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            ImGui.EndChild();
                        }

                        if (ImGui.BeginChild("##buttons", new(-1, 0), ImGuiChildFlags.FrameStyle | (ImGuiChildFlags)NoScrollFlags))
                        {
                            if (ImGui.Button("Close"))
                            {
                                Opened = false;
                                File.WriteAllText(ConfigName, StartDirectory);
                            }
                            ImGui.EndChild();
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Lua", ref open, TabIndex == 1 && isdeck ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                    {
                        ImGui.BeginGroup();
                        Enumerate(LuaDirectory);
                        for (int i = 0; i < GameFiles.Count; i++)
                        {
                            var file = GameFiles[i];
                            if (isdeck)
                            {
                                ImGui.SetWindowFocus();
                                RenderDeckMenu(1);
                                rlImGui.ImageRenderTextureFit(MenuRender);
                                if (Raylib.IsGamepadButtonPressed(0, BtnB))
                                {
                                    Lua.LoadLuaFile(GameFiles[Selection[1]].Name);
                                    Opened = false;
                                }
                            }
                            else
                            {
                                if (ImGui.Selectable(Path.GetFileName($"{file.Name}"), false, ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.DontClosePopups))
                                {
                                    if ((ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)))
                                    {
                                        if (File.Exists(file.Name))
                                        {
                                            Lua.LoadLuaFile(file.Name);
                                            ImGui.CloseCurrentPopup();
                                            Opened = false;
                                        }
                                    }
                                }
                            }
                        }
                        ImGui.EndGroup();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Cheats", ref open, TabIndex == 2 && isdeck ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                    {
                        ImGui.BeginGroup();

                        CheatsWindow.Render(Lua.Mmu);

                        ImGui.EndGroup();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
                ImGui.End();
            }
        }
    }

    private static void UpdateDeckMenu(int id)
    {
        int fontsize = 25;
        var width = MenuRender.Texture.Width;
        var height = MenuRender.Texture.Height;

        if (Raylib.IsGamepadButtonDown(0, BtnDown) || Raylib.IsGamepadButtonDown(0, BtnUp))
            DpadDelay++;
        else
            DpadDelay = 0;

        if (Raylib.IsGamepadButtonDown(0, BtnDown) && DpadDelay > 5)
        {
            var total = GameFiles.Count;
            if (++Selection[id] >= total)
                Selection[id] = total - 1;
            else
            {
                var bottomitem = height / fontsize;
                if (Selection[id] >= bottomitem && Selection[id] < total)
                    MenuScroll[id] -= fontsize;
            }
            DpadDelay = 0;
        }
        else if (Raylib.IsGamepadButtonDown(0, BtnUp) && DpadDelay > 5)
        {
            var y = Selection[id] * fontsize;
            if (Selection[id]-- <= 0)
                Selection[id] = 0;
            if (MenuScroll[id] < 0)
                MenuScroll[id] += fontsize;
            DpadDelay = 0;
        }
    }

    public static void RenderDeckMenu(int id)
    {
        Raylib.BeginTextureMode(MenuRender);

        int fontsize = 25;
        var width = MenuRender.Texture.Width;
        var height = MenuRender.Texture.Height;
        int posy = 0;
        var posx = 0;

        Raylib.BeginScissorMode(posx, posy, width, height);
        Raylib.DrawRectangle(posx, posy, width, height, new(0, 0, 0, 128));

        posy += MenuScroll[id];

        for (int i = 0; i < GameFiles.Count; i++)
        {
            var file = GameFiles[i];
            if (!file.IsDrive && file.IsFile)
            {
                if (i == Selection[id])
                {
                    Raylib.DrawRectangle(posx, posy, width, fontsize, new(0, 0, 0, 255));
                    Raylib.DrawText(Path.GetFileName(file.Name), posx, posy, fontsize + 1, Color.Yellow);
                }
                else
                    Raylib.DrawText(Path.GetFileName(file.Name), posx, posy, fontsize, Color.White);
            }
            posy += fontsize;
        }
        Raylib.EndScissorMode();

        Raylib.EndTextureMode();
    }

    public static void Enumerate(string path = "")
    {
        GameFiles.Clear();
        DirectoryInfo di = null;
        if (path != RomDirectory && path != LuaDirectory)
        {
            foreach (var file in DriveInfo.GetDrives())
            {
                if (file.IsReady)
                    GameFiles.Add(new(file.Name, file.IsReady, false));
            }
            di = new(StartDirectory);
            foreach (var file in di.EnumerateDirectories())
                GameFiles.Add(new(file.FullName, false, false));
            GameFiles.Insert(0, new("..", false, false));
        }
        else
            di = new(path);

        foreach (var file in di.EnumerateFiles())
        {
            var ext = file.Extension.ToLower();
            if (ext == ".gb" || ext == ".gbc" || ext == ".lua")
            {
                GameFiles.Add(new(file.FullName, false, true));
            }
        }
    }

    public static void Unload()
    {
        Raylib.UnloadRenderTexture(MenuRender);
    }
}
