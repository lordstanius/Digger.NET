using System;
using SDL2;

namespace Digger.Net
{
    class SdlGraphicsCga : SdlGraphics
    {
        public SdlGraphicsCga()
        {
            pal1 = CreatePalette(CgaGrafx.Palette1);
            pal1i = CreatePalette(CgaGrafx.Pallette1i);
            pal2 = CreatePalette(CgaGrafx.Paletter2);
            pal2i = CreatePalette(CgaGrafx.Palette2i);

            npalettes = new[] { pal1, pal2 };
            ipalettes = new[] { pal1i, pal2i };

            alphas = new Char2Surface(Alpha.ascii2cga);
            sprites = new Char2Surface(CgaGrafx.SpriteTable);
        }

        public override void GetImage(int x, int y, ref Surface tmp, int w, int h)
        {
            throw new NotImplementedException();
        }

        public override byte GetPixel(int x, int y)
        {
            throw new NotImplementedException();
        }

        public override void PutImage(int x, int y, Surface surface, int w, int h)
        {
            throw new NotImplementedException();
        }

        public override void PutImage(int x, int y, int ch, int w, int h)
        {
            throw new NotImplementedException();
        }

        public override void DrawTitleScreen()
        {
            DrawBackground(Properties.Resource.ctitle);
        }

        public override void WriteChar(int x, int y, int ch, int c)
        {
            throw new NotImplementedException();
        }
    }
}
