using GBoy.Gui;
using ImGuiNET;
using KeraLua;
using NLua;
using NLua.Exceptions;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Lua = NLua.Lua;

namespace GBoy.Core;
public class LuaApi
{
    public Mmu Mmu { get; private set; }

    public Lua Lua { get; private set; }

    private LuaMemory LuaMem;
    private LuaGui LuaGui;

    private string LuaCwd;
    private string Error;
    private string LuaFile;

    public LuaApi(Mmu mmu)
    {
        Mmu = mmu;
        Lua = new();
        LuaFile = "";
    }

    public void Update()
    {
        if (Lua.State != null)
        {
            try
            { Lua.GetFunction("emu.update").Call(); }
            catch (LuaScriptException e)
            {
                Notifications.Init(e.Message);
            }
        }

        if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftTrigger1))
        {
            LoadLuaFile($"{Path.GetFileNameWithoutExtension(Mmu.RomName)}.lua");
            if (Lua.State != null)
                Notifications.Init("Lua File Loaded Successfully");
        }
    }

    public void LoadLuaFile(string filename)
    {
        LuaFile = filename;

        Lua = new();

        Lua.NewTable("emu");
        ((LuaTable)Lua["emu"])["update"] = null;

        if (LuaCwd == "")
        {
            Directory.SetCurrentDirectory(@$"{Environment.CurrentDirectory}");
            LuaCwd = Environment.CurrentDirectory;
            LuaCwd = LuaCwd.Replace('\\', '/');
        }

        LuaMem = new(Lua, Mmu);
        LuaGui = new(Lua, Mmu);

        Lua.DoString(@"package.path = package.path ..';" + LuaCwd + "/?.lua'");
        try
        {
            if (File.Exists(@$"Lua/{filename}"))
                Lua.DoFile(@$"Lua/{filename}");
            else
            {
                Lua.Close();
                return;
            }
        }
        catch (LuaScriptException e)
        {
            Error = e.Message;
            Error += e.Source;
            LuaPrint(Error);
            Notifications.Init(Path.GetFileName(Error));
            return;
        }
        Error = "";
    }

    public static void LuaPrint(object text)
    {
        Console.WriteLine($"{text}");
    }

    public void Reset()
    {
        Lua.Close();
        LuaFile = "";
    }
    public void Unload() => Lua.Close();
}

public class LuaMemory
{
    private Mmu Mmu;

    public LuaMemory(Lua lua, Mmu mmu)
    {
        Mmu = mmu;
        lua.NewTable("mem");

        lua.RegisterFunction("mem.addcheat", this, typeof(LuaMemory).GetMethod("AddCheat"));
        lua.RegisterFunction("mem.readbyte", this, typeof(LuaMemory).GetMethod("ReadByte"));
    }

    public byte ReadByte(int a) => Mmu.Read(a, true);
    public void AddCheat(string name, string code)
    {
        if (code.Length < 9)
            return;
        if (name == null || CheatsWindow.ConvertCodes == null)
            return;

        CheatsWindow.ConvertCodes(name, code, Mmu, true);
    }
}

public class LuaGui
{
    private Mmu Mmu;

    public LuaGui(Lua lua, Mmu mmu)
    {
        Mmu = mmu;
        lua.NewTable("gui");

        lua.RegisterFunction("gui.drawwin", this, typeof(LuaGui).GetMethod("DrawWindow"));
        lua.RegisterFunction("gui.drawtext", this, typeof(LuaGui).GetMethod("DrawText"));
        lua.RegisterFunction("gui.drawrect", this, typeof(LuaGui).GetMethod("DrawRectangle"));

    }

    public static void DrawWindow(int x, int y, int w, int h, string name, LuaTable text)
    {
        var width = Raylib.GetScreenWidth();
        var height = Raylib.GetScreenHeight();
        var texwidth = GraphicsWindow.Screen.Texture.Width;
        var scale = Math.Min((float)width / texwidth, (float)height / GbHeight);
        var leftpos = (width - texwidth * scale) / 2;
        var t = text.Values;

        ImGui.SetNextWindowPos(new(x, GraphicsWindow.MenuHeight));
        ImGui.SetNextWindowSize(new(leftpos, h));
        if (ImGui.Begin(name))
        {
            foreach (var v in t)
            {
                ImGui.Text(v.ToString());
            }
            ImGui.End();
        }
    }

    public static void DrawText(params object[] args)
    {
        if (args.Length < 3)
            return;

        var c = "ffffffff";
        if ((args.Length == 4 && $"{args[3]}".Length == 8))
            c = $"{args[3]}";

        var x = Convert.ToInt32(args[0]);
        var y = Convert.ToInt32(args[1]);
        var text = $"{args[2]}";

        Raylib.DrawText(text, x, y, 14, GetColor(c));
    }

    public static void DrawRectangle(params object[] args)
    {
        if (args.Length != 5)
            return;

        var x = Convert.ToInt32(args[0]);
        var y = Convert.ToInt32(args[1]);
        var w = Convert.ToInt32(args[2]);
        var h = Convert.ToInt32(args[3]);
        if ($"{args[4]}".Length != 8)
            return;

        Raylib.DrawRectangle(x, y, w, h, GetColor(args[4]));
    }

    public static Color GetColor(object hexstr)
    {
        var c = Convert.ToInt32($"{hexstr}", 16);
        return new((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c);
    }
}
