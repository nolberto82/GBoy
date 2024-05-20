using Raylib_cs;
using System.Data;

namespace GBoy.Core;
public class Joypad
{
    private static byte status;
    public static byte Status
    {
        get => status;
        set
        {
            status = 0xef;
            if (!value.GetBit(4))
            {
                if (buttons[4])
                    status ^= 0x01;
                if (buttons[5])
                    status ^= 0x02;
                if (buttons[6])
                    status ^= 0x04;
                if (buttons[7])
                    status ^= 0x08;
            }
            else if (!value.GetBit(5))
            {
                if (buttons[0])
                    status ^= 0x01;
                if (buttons[1])
                    status ^= 0x02;
                if (buttons[2])
                    status ^= 0x04;
                if (buttons[3])
                    status ^= 0x08;
            }
        }
    }

    private static bool[] buttons;
    public bool this[int i]
    {
        get => buttons[i];
        set => buttons[i] = value;
    }

    static Joypad() => buttons = new bool[8];
    public byte Read()
    {
        byte v = 0;
        if (Status == 0x10)
        {
            v = (byte)((Status >> 4) | 0xf0);
        }
        else if (Status == 0x20)
        {
            v = (byte)((Status & 0x0f) | 0xf0);
        }
        return (byte)(v | 0xdf);
    }

    public static void Update(string name)
    {
        buttons[0] = Raylib.IsKeyDown(KbA);
        buttons[1] = Raylib.IsKeyDown(KbB);
        buttons[2] = Raylib.IsKeyDown(KbSelect);
        buttons[3] = Raylib.IsKeyDown(KbStart);
        buttons[4] = Raylib.IsKeyDown(KbRight);
        buttons[5] = Raylib.IsKeyDown(KbLeft);
        buttons[6] = Raylib.IsKeyDown(KbUp);
        buttons[7] = Raylib.IsKeyDown(KbDown);

        buttons[0] |= Raylib.IsKeyPressedRepeat(KbX);
        buttons[1] |= Raylib.IsKeyPressedRepeat(KbY);

        if (Raylib.IsGamepadAvailable(0))
        {
            //if (name.ToLower().Contains("super mario"))
            //{
            //    buttons[0] |= Raylib.IsGamepadButtonDown(0, BtnB);
            //     buttons[1] |= Raylib.IsGamepadButtonDown(0, BtnY);
            //}
            //else
            //{
            buttons[0] |= Raylib.IsGamepadButtonDown(0, BtnA);
            buttons[1] |= Raylib.IsGamepadButtonDown(0, BtnB);
            //}

            buttons[2] |= Raylib.IsGamepadButtonDown(0, BtnSelect);
            buttons[3] |= Raylib.IsGamepadButtonDown(0, BtnStart);
            buttons[4] |= Raylib.IsGamepadButtonDown(0, BtnRight);
            buttons[5] |= Raylib.IsGamepadButtonDown(0, BtnLeft);
            buttons[6] |= Raylib.IsGamepadButtonDown(0, BtnUp);
            buttons[7] |= Raylib.IsGamepadButtonDown(0, BtnDown);
            //buttons[0] |= Raylib.IsGamepadButtonDown(0, BtnX);
            //buttons[1] |= Raylib.IsGamepadButtonDown(0, BtnY);
        }
    }
}
