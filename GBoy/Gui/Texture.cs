﻿using Raylib_cs;
using Image = Raylib_cs.Image;

namespace GBoy.Gui
{
    public static class Texture
    {
        public static unsafe void Update(Texture2D texture, uint[] buffer)
        {
            //void* pixels = Unsafe.AsPointer(ref buffer.DangerousGetReference());
            fixed (uint* pixels = &buffer[0])
                Raylib.UpdateTexture(texture, pixels);
        }

        public static unsafe Texture2D CreateTexture(uint[] buffer, int w, int h)
        {
            fixed (uint* pixels = &buffer[0])
            {
                Image img = new Image
                {
                    Data = pixels,
                    Width = w,
                    Height = h,
                    Format = PixelFormat.UncompressedR8G8B8A8,
                    Mipmaps = 1,
                };
                return Raylib.LoadTextureFromImage(img);
            }
        }
    }
}
