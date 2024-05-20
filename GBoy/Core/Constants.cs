﻿using ImGuiNET;
using Raylib_cs;
using System.Numerics;

namespace GBoy.Core;
public static class Constants
{
    public const int FC = 0x10;
    public const int FH = 0x20;
    public const int FN = 0x40;
    public const int FZ = 0x80;

    public const int Debugging = 0;
    public const int Stepping = 1;
    public const int Running = 2;
    public const int Paused = 3;

    public const int CpuClock = 4194304;
    public const int CyclesPerFrame = 70224;

    public const int IntVblank = 0x01;
    public const int IntLcd = 0x02;
    public const int IntTimer = 0x04;
    public const int IntSerial = 0x08;
    public const int IntJoypad = 0x10;

    public const int GbWidth = 160;
    public const int GbHeight = 144;

    public const int GameGenie = 0;
    public const int GameShark = 1;

    public static readonly Vector2 ButtonSize = new(70, 0);
    public static readonly Vector4 RED = new(1, 0, 0, 1);
    public static readonly Vector4 GREEN = new(0, 1, 0, 1);
    public static readonly Vector4 BLUE = new(0, 0, 1, 1);
    public static readonly Vector4 WHITE = new(1, 1, 1, 1);
    public static readonly Vector4 DEFCOLOR = new(0.260f, 0.590f, 0.980f, 0.400f);
    public const ImGuiInputTextFlags HexInputFlags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase | ImGuiInputTextFlags.CallbackCompletion;
    public const ImGuiWindowFlags NoScrollFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    public const ImGuiWindowFlags DockFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoCollapse |
        ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize;

    public const KeyboardKey KbA = KeyboardKey.Z;
    public const KeyboardKey KbB = KeyboardKey.X;
    public const KeyboardKey KbSelect = KeyboardKey.Space;
    public const KeyboardKey KbStart = KeyboardKey.Enter;
    public const KeyboardKey KbRight = KeyboardKey.Right;
    public const KeyboardKey KbLeft = KeyboardKey.Left;
    public const KeyboardKey KbUp = KeyboardKey.Up;
    public const KeyboardKey KbDown = KeyboardKey.Down;
    public const KeyboardKey KbX = KeyboardKey.C;
    public const KeyboardKey KbY = KeyboardKey.B;

    public const GamepadButton BtnA = GamepadButton.RightFaceRight;
    public const GamepadButton BtnB = GamepadButton.RightFaceDown;
    public const GamepadButton BtnSelect = GamepadButton.MiddleLeft;
    public const GamepadButton BtnStart = GamepadButton.MiddleRight;
    public const GamepadButton BtnRight = GamepadButton.LeftFaceRight;
    public const GamepadButton BtnLeft = GamepadButton.LeftFaceLeft;
    public const GamepadButton BtnUp = GamepadButton.LeftFaceUp;
    public const GamepadButton BtnDown = GamepadButton.LeftFaceDown;
    public const GamepadButton BtNR2 = GamepadButton.RightTrigger2;
    public const GamepadButton BtnX = GamepadButton.RightFaceUp;
    public const GamepadButton BtnY = GamepadButton.RightFaceLeft;

    public static readonly string RomDirectory = "Roms";
    public static readonly string SaveDirectory = "Saves";
    public static readonly string StateDirectory = "States";
    public static readonly string CheatDirectory = "Cheats";
    public static readonly string LuaDirectory = "Lua";
    public static readonly string ConfigDirectory = "Config";

    public static readonly Dictionary<int, string> MapperTypes = new()
    {
        [0x00] = "ROM ONLY",
        [0x01] = "MBC1",
        [0x02] = "MBC1RAM",
        [0x03] = "MBC1RAMBATTERY",
        [0x05] = "MBC2",
        [0x06] = "MBC2BATTERY",
        [0x08] = "ROMRAM 1",
        [0x09] = "ROMRAMBATTERY 1",
        [0x0B] = "MMM01",
        [0x0C] = "MMM01RAM",
        [0x0D] = "MMM01RAMBATTERY",
        [0x0F] = "MBC3TIMERBATTERY",
        [0x10] = "MBC3TIMERRAMBATTERY 2",
        [0x11] = "MBC3",
        [0x12] = "MBC3RAM 2",
        [0x13] = "MBC3RAMBATTERY 2",
        [0x19] = "MBC5",
        [0x1A] = "MBC5RAM",
        [0x1B] = "MBC5RAMBATTERY",
        [0x1C] = "MBC5RUMBLE",
        [0x1D] = "MBC5RUMBLERAM",
        [0x1E] = "MBC5RUMBLERAMBATTERY",
        [0x20] = "MBC6",
        [0x22] = "MBC7SENSORRUMBLERAMBATTERY",
        [0xFC] = "POCKET CAMERA",
        [0xFD] = "BANDAI TAMA5",
        [0xFE] = "HuC3",
        [0xFF] = "HuC1RAMBATTERY",
    };
}
