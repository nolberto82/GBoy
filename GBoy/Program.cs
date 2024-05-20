
using GBoy.Core.MBCs;
using GBoy.Gui;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using System.Text.Json;
using Color = Raylib_cs.Color;
using Timer = GBoy.Core.Timer;

namespace GBoy.Gui;
public class Program
{
    public Cpu Cpu { get; private set; }
    public Ppu Ppu { get; private set; }
    public Apu Apu { get; private set; }
    public Mmu Mmu { get; private set; }
    public MBC Mbc { get; private set; }
    public IO IO { get; private set; }
    public Cartridge Cart { get; private set; }
    public Timer Timer { get; private set; }
    public Logger Logger { get; private set; }
    public DebugWindow DebugWindow { get; private set; }
    public Breakpoint Breakpoint { get; private set; } = new();
    public List<Breakpoint> Breakpoints { get; private set; } = new();
    public Joypad Joypad { get; private set; } = new();
    public LuaApi LuaApi { get; private set; }
    public Audio Audio { get; private set; }

    private float OldRightThumbUp;
    private float OldRightThumbDown;

    public ImFontPtr Consolas { get; private set; }

    public static bool FastForward { get; private set; }

    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 800;
    private readonly string WindowTitle = "GBoy";
    private const string FontName = "Fonts/consola.ttf";

    static void Main() => new Program().Init();

    public void Init()
    {
        Timer = new();
        IO = new(Timer);
        Mmu = new(IO, Breakpoint);
        Cart = new();
        Cpu = new(Mmu, IO, Tick);
        Ppu = new(Mmu, IO);
        Apu = new(Mmu);
        Logger = new(Mmu, Cpu);
        LuaApi = new(Mmu);

        Mbc = new MBC0();
        Mbc.Init(new byte[0x8000], false);
        Mmu.Init(Cpu, Mbc);
        IO.Init(Mmu, Ppu, Apu);

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, WindowTitle);
        Raylib.SetTargetFPS(60);

#if DEBUG || DECKDEBUG
        //Raylib.SetTargetFPS(0);
        Raylib.SetWindowPosition(10, 30);
        Raylib.ClearWindowState(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
#endif

        Audio = new();
        GraphicsWindow.Init(this);
        rlImGui.Setup(true, true);

        DebugWindow = new(this);

        var io = ImGui.GetIO();
        unsafe
        {
            io.NativePtr->IniFilename = null;
            ImGui.LoadIniSettingsFromMemory(IniSettings);
        }

        ImGui.GetIO().Fonts.AddFontDefault();
        if (File.Exists(FontName))
        {
            Consolas = ImGui.GetIO().Fonts.AddFontFromFileTTF(FontName, 15f);
            rlImGui.ReloadFonts();
        }

        if (!Directory.Exists(RomDirectory))
            Directory.CreateDirectory(RomDirectory);

        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);

        if (!Directory.Exists(StateDirectory))
            Directory.CreateDirectory(StateDirectory);

        if (!Directory.Exists(CheatDirectory))
            Directory.CreateDirectory(CheatDirectory);

        if (!Directory.Exists(LuaDirectory))
            Directory.CreateDirectory(LuaDirectory);

        if (!Directory.Exists(ConfigDirectory))
            Directory.CreateDirectory(ConfigDirectory);

        LoadConfig();
        Run();
    }

    private void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.F4))
                Raylib.TakeScreenshot($"screenshot{DateTime.Now.Ticks}.png");

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGray);

            rlImGui.Begin();

            GamepadInputs();

            Input();
            Execute();
            GraphicsWindow.Render(Consolas, Logger, DebugWindow, Apu);

            rlImGui.End();
            Raylib.EndDrawing();
        }

        Cart.Save(Mmu.Sram);
        CheatsWindow.Save(Cart.Name);
        SaveConfig();

        LuaApi.Unload();

        Audio.Unload();
        GraphicsWindow.Unload();
        FileBrowser.Unload();
        //Audio.Unload();
        rlImGui.Shutdown();

        Raylib.CloseWindow();
    }

    private void Execute()
    {
        if (Cpu.State == Running && Mmu.RomLoaded)
        {
            var cyclesframe = CyclesPerFrame;
            while (Cpu.Cycles < cyclesframe)
            {
                ushort pc = Cpu.PC;
                if (Breakpoint.BPs.Count > 0)
                    CheckBreakpoints(pc);
                if (Cpu.State == Debugging)
                    return;

                if (Logger.Logging)
                    Logger.LogToFile(pc);

                Cpu.Step(pc);
                if (Cpu.State == Debugging)
                    return;
            }
            Cpu.Cycles -= cyclesframe;
        }
    }

    public void Tick()
    {
        var c = 4 / IO.SpeedMode;
        Cpu.Cycles += c;
        Timer.Step(IO, Cpu, 4);
        Ppu.Step(c);
        if (!FastForward)
            Apu.Step(c);
    }

    private void Input()
    {
        if (!Raylib.IsWindowFocused()) return;
        var NewL2Button = Raylib.IsGamepadButtonDown(0, GamepadButton.LeftTrigger2);
        var NewRightStickUp = Raylib.GetGamepadAxisMovement(0, GamepadAxis.RightY);
        var NewRightStickDown = Raylib.GetGamepadAxisMovement(0, GamepadAxis.RightY);

        Joypad.Update(Cart.Name);

        if (Raylib.IsGamepadButtonDown(0, GamepadButton.RightTrigger2) && !FastForward)
        {
            Raylib.SetTargetFPS(0);
            FastForward = true;
            Raylib.ClearWindowState(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
        }
        else if (!Raylib.IsGamepadButtonDown(0, GamepadButton.RightTrigger2) && FastForward)
        {
            FastForward = false;
            Raylib.SetTargetFPS(60);

#if !DEBUG && !DECKDEBUG
            Raylib.SetWindowState(ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
#endif
        }

        //Open Filebrowser L2
        if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftTrigger2) && GraphicsWindow.IsDeck)
        {
            FileBrowser.Opened = !FileBrowser.Opened;
            if (FileBrowser.Opened)
                Cpu.State = Paused;
            else
                Cpu.State = Running;
        }

        if (NewRightStickUp < 0 && OldRightThumbUp == 0)
            SaveState.Save(this);
        else if (NewRightStickDown > 0 && OldRightThumbDown == 0)
            SaveState.Load(this);

        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (shift && Raylib.IsKeyPressed(KeyboardKey.F1))
            SaveState.Save(this);
        else if (Raylib.IsKeyPressed(KeyboardKey.F1))
            SaveState.Load(this);

        OldRightThumbUp = NewRightStickUp;
        OldRightThumbDown = NewRightStickDown;
    }

    private void CheckBreakpoints(ushort pc)
    {
        if ((ushort)Cpu.StepOverAddr == pc)
        {
            Cpu.StepOverAddr = -1;
            Cpu.State = Debugging;
            return;
        }

        if (Cpu.StepOverAddr == -1 && Breakpoint.Execution(pc))
        {
            Cpu.StepOverAddr = -1;
            Cpu.State = Debugging;
            return;
        }
    }

    public void ContinueExecution()
    {
        if (Cpu == null || Mmu == null) return;
        Cpu.State = Running;
        Cpu.StepOverAddr = -1;

        if (Logger.Logging)
            Logger.LogToFile(Cpu.PC);

        Cpu.Step(Cpu.PC);
    }

    public void OpenFile(string filename)
    {
        Mbc = Mmu.LoadRom(Cart, filename);
        if (Mbc != null)
            Reset();
    }

    public void Reset()
    {
        var sram = Mmu.Sram.AsSpan().ToArray();
        Cpu.Reset(Cart.IsBios, Cart.CGB, GraphicsWindow.DebuggerVisible);
        Ppu.Reset(Cart.CGB);
        Apu.Reset();
        Mmu.Reset(Cart.CGB);
        Cart.Reset(ref sram);
        IO.Reset();
        Logger.Reset();
        LuaApi.Reset();
        Texture.Update(GraphicsWindow.Screen.Texture, Ppu.ScreenBuffer);
    }

    private static void GamepadInputs()
    {
        var io = ImGui.GetIO();

        if ((io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) > 0)
        {
            RemapButton(io, BtnA, ImGuiKey.GamepadFaceUp);
        }
    }

    private static void RemapButton(ImGuiIOPtr io, GamepadButton button, ImGuiKey key)
    {
        if (Raylib.IsGamepadButtonDown(0, button))
            io.AddKeyEvent(key, false);
        if (Raylib.IsGamepadButtonPressed(0, button))
            io.AddKeyEvent(key, true);
        else if (Raylib.IsGamepadButtonReleased(0, button))
            io.AddKeyEvent(key, false);
    }

    public void LoadConfig()
    {
        var file = @$"{ConfigDirectory}/Settings.json";
        if (File.Exists(file))
        {
            var res = JsonSerializer.Deserialize<List<Config>>(File.ReadAllText(file));
            if (res.Count > 0)
            {
                FileBrowser.StartDirectory = res[0].Directory;
                Raylib.SetMasterVolume(res[0].Volume);
            }
        }
    }

    public void SaveConfig()
    {
        List<Config> config = [new(FileBrowser.StartDirectory, Raylib.GetMasterVolume())];
        var file = @$"{ConfigDirectory}/Settings.json";
        JsonSerializerOptions options = new() { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(file, json);
    }
}

public class Config
{
    public string Directory { get; set; }
    public float Volume { get; set; }

    public Config(string directory, float volume)
    {
        Directory = directory;
        Volume = volume;
    }
}