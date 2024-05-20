using ImGuiNET;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace GBoy.Gui
{
    internal static class Notifications
    {
        private static string[] Text;
        private static int Frames;

        public static void Init(string text)
        {
            Text = text.Split(new[] { ": ", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Frames = 125;
        }

        public static void Render(int x, int y, int width, int height, bool isdeck)
        {
            if (Frames-- > 0)
                GraphicsWindow.RenderText(Text, x, y, width, height, Color.Yellow, isdeck);
        }

        public static void RenderDebug(ImFontPtr consolas, int x, int y, int width, int height, bool isdeck)
        {
            if (Frames-- > 0)
            {
                var list = ImGui.GetForegroundDrawList();
                var pos = ImGui.GetWindowPos();
                var size = ImGui.GetWindowSize();
                list.AddRectFilled(pos, new(pos.X + size.X, pos.Y + 5 + 15 * Text.Length), 0xc0000000);
                foreach (var text in Text)
                    list.AddText(consolas, 16, new(pos.X + 5, pos.Y + 5), 0xff00ffff, text);
            }
        }
    }
}
