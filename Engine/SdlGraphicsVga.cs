/*
 * ---------------------------------------------------------------------------
 * "THE BEER-WARE LICENSE" (Revision 42, (c) Poul-Henning Kamp): Maxim
 * Sobolev <sobomax@altavista.net> wrote this file. As long as you retain
 * this notice you can do whatever you want with this stuff. If we meet
 * some day, and you think this stuff is worth it, you can buy me a beer in
 * return.
 * 
 * Maxim Sobolev
 * --------------------------------------------------------------------------- 
 */

using SDL2;
using System;
using System.Runtime.InteropServices;

namespace Digger.Net
{
    public class SdlGraphicsVga : SdlGraphics
    {
        private const int xratio = 2;
        private const int yratio = 2;
        private const int yoffset = 0;
        private const int hratio = 2;
        private const int wratio = 2 * 4;

        public SdlGraphicsVga()
        {
            pal1 = CreatePalette(VgaGrafx.Palette1);
            pal1i = CreatePalette(VgaGrafx.Palette1i);
            pal2 = CreatePalette(VgaGrafx.Palette2);
            pal2i = CreatePalette(VgaGrafx.Palette2i);

            npalettes = new[] { pal1, pal2 };
            ipalettes = new[] { pal1i, pal2i };

            alphas = new Char2Surface(Alpha.ascii2vga);
            sprites = new Char2Surface(VgaGrafx.SpriteTable);
        }

        public override void PutImage(int x, int y, Surface surface, int w, int h)
        {
            SDL.SDL_Rect rect = new SDL.SDL_Rect
            {
                x = virt2scrx(x),
                y = virt2scry(y),
                w = virt2scrw(w),
                h = virt2scrh(h)
            };

            IntPtr reserv = surface.Format.palette;
            surface.SetSurfacePalette(screen16.Format.palette);
            surface.Blit(IntPtr.Zero, screen16, ref rect);
            surface.SetSurfacePalette(reserv);
        }

        /// <summary>
        /// Get image on given coordinates, with given size
        /// </summary>
        public override void GetImage(int x, int y, ref Surface surface, int w, int h)
        {
            SDL.SDL_Rect src = new SDL.SDL_Rect
            {
                x = virt2scrx(x),
                y = virt2scry(y),
                w = virt2scrw(w),
                h = virt2scrh(h)
            };

            if (surface == null)
            {
                surface = Surface.CreateRGBSurface(0, src.w, src.h, 8, 0, 0, 0, 0);
                SDL.SDL_SetSurfacePalette(surface, screen16.Format.palette);
            }

            screen16.Blit(ref src, surface, IntPtr.Zero);
        }

        public override byte GetPixel(int x, int y)
        {
            short rval = 0;
            if ((x > 319) || (y > 199))
                return (0xff);

            int i = 0;
            Surface surface = null;
            GetImage(x, y, ref surface, 1, 1);
            IntPtr pixels = surface.pixels;
            for (int yi = 0; yi < surface.h; yi++)
                for (int xi = 0; xi < surface.w; xi++)
                    if (Marshal.ReadByte(surface.pixels, i++) != 0)
                        rval |= (short)(0x80 >> xi);

            surface.Free();

            return (byte)(rval & 0xee);
        }

        public override void PutImage(int x, int y, int ch, int w, int h)
        {
            Surface tmp = ch2bmap(ref sprites, ch * 2, w, h);
            Surface mask = ch2bmap(ref sprites, ch * 2 + 1, w, h);

            Surface scr = null;
            GetImage(x, y, ref scr, w, h);
            int size = tmp.w * tmp.h;
            byte[] tmp_pxl = new byte[size];
            byte[] mask_pxl = new byte[size];
            byte[] scr_pxl = new byte[size];

            Marshal.Copy(tmp.pixels, tmp_pxl, 0, size);
            Marshal.Copy(mask.pixels, mask_pxl, 0, size);
            Marshal.Copy(scr.pixels, scr_pxl, 0, size);

            for (int i = 0; i < size; i++)
            {
                if (tmp_pxl[i] != 0xff)
                    scr_pxl[i] = (byte)((scr_pxl[i] & mask_pxl[i]) | tmp_pxl[i]);
            }

            Marshal.Copy(scr_pxl, 0, scr.pixels, size);
            PutImage(x, y, scr, w, h);
            scr.Free();
        }

        public override void WriteChar(int x, int y, int ch, int c)
        {
            int w = 3, h = 12;

            if (!Alpha.IsValidChar(ch))
                return;

            Surface tmp = ch2bmap(ref alphas, ch - 32, w, h);
            int size = tmp.w * tmp.h;
            byte[] copy = new byte[size];
            Marshal.Copy(tmp.pixels, copy, 0, copy.Length);

            for (int i = 0; i < size; ++i)
            {
                byte color = copy[i];
                if (color == 10)
                {
                    if (c == 2)
                        color = 12;
                    else if (c == 3)
                        color = 14;
                }
                else if (color == 12)
                {
                    if (c == 1)
                        color = 2;
                    else if (c == 2)
                        color = 4;
                    else if (c == 3)
                        color = 6;
                }
                copy[i] = color;
            }
            // save original pixels
            IntPtr originalPixels = Marshal.AllocHGlobal(size);
            StdLib.MemCpy(originalPixels, tmp.pixels, size);

            Marshal.Copy(copy, 0, tmp.pixels, size);
            PutImage(x, y, tmp, w, h);

            StdLib.MemCpy(tmp.pixels, originalPixels, size);
            Marshal.FreeHGlobal(originalPixels);
        }

        public override void DrawTitleScreen()
        {
            DrawBackground(Properties.Resource.vtitle);
        }

        private Surface ch2bmap(ref Char2Surface planep, int sprite, int w, int h)
        {
            if (planep.caches[sprite] != null)
                return (planep.caches[sprite]);

            int realw = virt2scrw(w);
            int realh = virt2scrh(h);
            int size = realw * realh;
            IntPtr pixels = Marshal.AllocHGlobal(size);
            Marshal.Copy(planep.sprites[sprite], 0, pixels, size);
            Surface surface = Surface.CreateRGBSurfaceFrom(pixels, realw, realh, 8, realw, 0, 0, 0, 0);
            SDL.SDL_SetPaletteColors(surface.Format.palette, npalettes[0], 0, 16);
            planep.caches[sprite] = surface;

            return surface;
        }

        private int virt2scrx(int x) => x * xratio;
        private int virt2scry(int y) => y * yratio + yoffset;
        private int virt2scrw(int w) => w * wratio;
        private int virt2scrh(int h) => h * hratio;
    }
}