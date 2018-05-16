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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        private const int xratio = 2;
        private const int yratio = 2;
        private const int yoffset = 0;
        private const int hratio = 2;
        private const int wratio = 2 * 4;
        private static int virt2scrx(int x) => x * xratio;
        private static int virt2scry(int y) => y * yratio + yoffset;
        private static int virt2scrw(int w) => w * wratio;
        private static int virt2scrh(int h) => h * hratio;

        /* palette1, normal intensity */
        static readonly byte[,] vga16_pal1_data = {
            {0,0,0,0},
            {0,0,128,0},
            {0,128,0,0},
            {0,128,128,0},
            {128,0,0,0},
            {128,0,128,0},
            {128,64,0,0},
            {128,128,128,0},
            {64,64,64,0},
            {0,0,255,0},
            {0,255,0,0},
            {0,255,255,0},
            {255,0,0,0},
            {255,0,255,0},
            {255,255,0,0},
            {255,255,255,0}
        };

        /* palette1, high intensity */
        static readonly byte[,] vga16_pal1i_data = {
            {0,0,0,0},
            {0,0,255,0},
            {0,255,0,0},
            {0,255,255,0},
            {255,0,0,0},
            {255,0,255,0},
            {255,128,0,0},
            {196,196,196,0},
            {128,128,128,0},
            {128,128,255,0},
            {128,255,128,0},
            {128,255,255,0},
            {255,128,128,0},
            {255,128,255,0},
            {255,255,128,0},
            {255,255,255,0}
        };

        /* palette2, normal intensity */
        static readonly byte[,] vga16_pal2_data = {
            {0,0,0,0},
            {0,128,0,0},
            {128,0,0,0},
            {128,64,0,0},
            {0,0,128,0},
            {0,128,128,0},
            {128,0,128,0},
            {128,128,128,0},
            {64,64,64,0},
            {0,255,0,0},
            {255,0,0,0},
            {255,255,0,0},
            {0,0,255,0},
            {0,255,255,0},
            {255,0,255,0},
            {255,255,255,0}
        };

        /* palette2, high intensity */
        static readonly byte[,] vga16_pal2i_data = {
            {0,0,0,0},
            {0,255,0,0},
            {255,0,0,0},
            {255,128,0,0},
            {0,0,255,0},
            {0,255,255,0},
            {255,0,255,0},
            {196,196,196,0},
            {128,128,128,0},
            {128,255,128,0},
            {255,128,128,0},
            {255,255,128,0},
            {128,128,255,0},
            {128,255,255,0},
            {255,128,255,0},
            {255,255,255,0}
        };

        // TODO: Reverse order of colors
        /* palette1, normal intensity */
        public static byte[,] cga16_pal1_data = {
            {0,0,0,0},
            {0,168,0,0},
            {0,0,168,0},
            {0,84,168,0},
            {0,0,128,0},
            {128,0,128,0},
            {0,64,128,0},
            {128,128,128,0},
            {64,64,64,0},
            {255,0,0,0},
            {0,255,0,0},
            {255,255,0,0},
            {0,0,255,0},
            {255,0,255,0},
            {0,255,255,0},
            {255,255,255,0} };

        /* palette1, high intensity */
        public static byte[,] cga16_pal1i_data = {
            {0,0,0,0},
            {85,255,85,0},
            {85,85,255,0},
            {85,255,255,0},
            {0,0,255,0},
            {255,0,255,0},
            {0,128,255,0},
            {192,192,192,0},
            {128,128,128,0},
            {255,128,128,0},
            {128,255,128,0},
            {255,255,128,0},
            {128,128,255,0},
            {255,128,255,0},
            {128,255,255,0},
            {255,255,255,0} };

        /* palette2, normal intensity */
        public static byte[,] cga16_pal2_data = {
            {0,0,0,0},
            {0,128,0,0},
            {128,0,128,0},
            {160,160,160,0},
            {160,160,160,0},
            {128,128,0,0},
            {128,0,128,0},
            {128,128,128,0},
            {64,64,64,0},
            {0,255,0,0},
            {0,0,255,0},
            {0,255,255,0},
            {255,0,0,0},
            {255,255,0,0},
            {255,0,255,0},
            {255,255,255,0} };

        /* palette2, high intensity */
        public static byte[,] cga16_pal2i_data = {
            {0,0,0,0},
            {0,255,0,0},
            {0,0,255,0},
            {160,160,160,0},
            {255,0,0,0},
            {255,255,0,0},
            {255,0,255,0},
            {192,192,192,0},
            {128,128,128,0},
            {128,255,128,0},
            {128,128,255,0},
            {128,255,255,0},
            {255,128,128,0},
            {255,255,128,0},
            {255,128,255,0},
            {255,255,255,0} };

        static readonly SDL.Color[] vga16_pal1 = CreatePalette(vga16_pal1_data);
        static readonly SDL.Color[] vga16_pal1i = CreatePalette(vga16_pal1i_data);
        static readonly SDL.Color[] vga16_pal2 = CreatePalette(vga16_pal2_data);
        static readonly SDL.Color[] vga16_pal2i = CreatePalette(vga16_pal2i_data);

        private static SDL.Color[] CreatePalette(byte[,] colors)
        {
            var pallete = new SDL.Color[colors.GetLength(0)];
            for (int i = 0; i < colors.GetLength(0); ++i)
                pallete[i] = new SDL.Color()
                {
                    r = colors[i, 0],
                    g = colors[i, 1],
                    b = colors[i, 2],
                    a = colors[i, 3]
                };

            return pallete;
        }

        static SDL.Color[][] npalettes = { vga16_pal1, vga16_pal2 };
        static SDL.Color[][] ipalettes = { vga16_pal1i, vga16_pal2i };
        static int currpal = 0;

        const uint SDL_FULLSCREEN = (uint)SDL.WindowFlags.WINDOW_FULLSCREEN;
        static uint addflag = 0;

        static IntPtr window;
        static IntPtr renderer;
        static IntPtr roottxt;
        static Surface screen;
        static Surface screen16;

        struct ch2bmap_plane
        {
            public ch2bmap_plane(byte[][] table)
            {
                caches = new Surface[256];
                sprites = table;
            }

            public byte[][] sprites;
            public Surface[] caches;
        };

        static ch2bmap_plane sprites;
        static ch2bmap_plane alphas;

        static Surface ch2bmap(ref ch2bmap_plane planep, int sprite, int w, int h)
        {
            if (planep.caches[sprite] != null)
                return (planep.caches[sprite]);

            int realw = virt2scrw(w);
            int realh = virt2scrh(h);
            byte[] pixels = planep.sprites[sprite];
            Surface surface = Surface.CreateRGBSurface(0, realw, realh, 8, 0, 0, 0, 0);
            Marshal.Copy(pixels, 0, surface.pixels, pixels.Length);
            SDL.SetPaletteColors(surface.Format.palette, npalettes[0], 0, 16);
            planep.caches[sprite] = surface;

            return surface;
        }

        public static void graphicsoff()
        {
        }

        static bool setmode()
        {
            if ((addflag & SDL_FULLSCREEN) != 0)
                SDL.SetWindowFullscreen(window, SDL.WindowFlags.WINDOW_FULLSCREEN_DESKTOP);
            else
                SDL.SetWindowFullscreen(window, 0);

            return (true);
        }

        public static void switchmode()
        {
            uint saved;

            saved = addflag;

            if ((addflag & SDL_FULLSCREEN) == 0)
            {
                addflag |= SDL_FULLSCREEN;
            }
            else
            {
                addflag &= ~SDL_FULLSCREEN;
            }
            if (setmode() == false)
            {
                addflag = saved;
                if (setmode() == false)
                {
                    Log.Write("Fatal: failed to change videomode and fallback mode failed as well. Exitting.");
                    Environment.Exit(1);
                }
            }
        }

        public static void vgainit()
        {
            if (SDL.Init(SDL.INIT_VIDEO) < 0)
            {
                Log.Write($"Couldn't initialize SDL: {SDL.GetError()}");
                Environment.Exit(1);
            }

            window = SDL.CreateWindow("Digger", SDL.WINDOWPOS_UNDEFINED, SDL.WINDOWPOS_UNDEFINED, 640, 400, 0);
            if (window == null)
            {
                Log.Write($"SDL_CreateWindow() failed: {SDL.GetError()}");
                Environment.Exit(1);
            }

            renderer = SDL.CreateRenderer(window, -1, 0);
            if (renderer == null)
            {
                Log.Write($"SDL_CreateRenderer() failed: {SDL.GetError()}");
                Environment.Exit(1);
            }

            roottxt = SDL.CreateTexture(renderer, SDL.PIXELFORMAT_ARGB8888, SDL.TextureAccess.TEXTUREACCESS_STREAMING, 640, 400);
            if (roottxt == null)
            {
                Log.Write($"SDL_CreateTexture() failed: {SDL.GetError()}");
                Environment.Exit(1);
            }
            screen = Surface.CreateRGBSurface(0, 640, 400, 32,
                0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
            if (screen == null)
            {
                Log.Write($"SDL_CreateRGBSurface() failed: {SDL.GetError()}");
                Environment.Exit(1);
            }
            screen16 = Surface.CreateRGBSurface(0, 640, 400, 8, 0, 0, 0, 0);
            if (screen16 == null)
            {
                Log.Write($"SDL_CreateRGBSurface() failed: {SDL.GetError()}");
                Environment.Exit(1);
            }

            if (setmode() == false)
            {
                Log.Write($"Couldn't set 640x400x8 video mode: {SDL.GetError()}");
                Environment.Exit(1);
            }

            SDL.ShowCursor(1);

            alphas = new ch2bmap_plane(ascii2vga);
            sprites = new ch2bmap_plane(vgatable);
        }

        public static void vgaclear()
        {
            Surface tmp = null;
            vgageti(0, 0, ref tmp, 80, 200);
            byte[] empty = new byte[tmp.w * tmp.h];
            Marshal.Copy(empty, 0, tmp.pixels, empty.Length);
            vgaputi(0, 0, tmp, 80, 200);
            tmp.Free();
        }

        static void setpal(SDL.Color[] pal)
        {
            SDL.SetPaletteColors(screen16.Format.palette, pal, 0, 16);
        }

        public static void vgainten(int inten)
        {
            if (inten == 1)
                setpal(ipalettes[currpal]);
            else
                setpal(npalettes[currpal]);
        }

        public static void vgapal(int pal)
        {
            setpal(npalettes[pal]);
            currpal = pal;
        }

        public static void doscreenupdate()
        {
            screen16.Blit(IntPtr.Zero, screen, IntPtr.Zero);
            SDL.UpdateTexture(roottxt, IntPtr.Zero, screen.pixels, screen.pitch);
            SDL.RenderClear(renderer);
            SDL.RenderCopy(renderer, roottxt, IntPtr.Zero, IntPtr.Zero);
            SDL.RenderPresent(renderer);
        }

        public static void vgaputi(int x, int y, Surface surface, int w, int h)
        {
            SDL.Rect rect = new SDL.Rect
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
        public static void vgageti(int x, int y, ref Surface surface, int w, int h)
        {
            SDL.Rect src = new SDL.Rect
            {
                x = virt2scrx(x),
                y = virt2scry(y),
                w = virt2scrw(w),
                h = virt2scrh(h)
            };

            if (surface == null)
            {
                surface = Surface.CreateRGBSurface(0, src.w, src.h, 8, 0, 0, 0, 0);
                SDL.SetPaletteColors(surface.Format.palette, screen16.Format.Palette.Colors, 0, 16);
            }

            screen16.Blit(ref src, surface, IntPtr.Zero);
        }

        public static short vgagetpix(int x, int y)
        {
            short rval = 0;
            if ((x > 319) || (y > 199))
                return (0xff);

            int i = 0;
            Surface surface = null;
            vgageti(x, y, ref surface, 1, 1);
            IntPtr pixels = surface.pixels;
            for (int yi = 0; yi < surface.h; yi++)
                for (int xi = 0; xi < surface.w; xi++)
                    if (IntPtr.Add(pixels, i++).ToInt32() != 0)
                        rval |= (short)(0x80 >> xi);

            surface.Free();

            return (short)(rval & 0xee);
        }

        public static void vgaputim(int x, int y, int ch, int w, int h)
        {
            Surface tmp = ch2bmap(ref sprites, ch * 2, w, h);
            Surface mask = ch2bmap(ref sprites, ch * 2 + 1, w, h);

            Surface scr = null;
            vgageti(x, y, ref scr, w, h);
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
            vgaputi(x, y, scr, w, h);
            scr.Free();
        }

        public static void vgawrite(int x, int y, int ch, int c)
        {
            int w = 3, h = 12;

            if (!isvalchar(ch))
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
            vgaputi(x, y, tmp, w, h);

            StdLib.MemCpy(tmp.pixels, originalPixels, size);
            Marshal.FreeHGlobal(originalPixels);
        }

        public static void vgatitle()
        {
            DrawBackground(Properties.Resource.vtitle);
        }

        public static void gretrace()
        {
        }

        public static void savescreen()
        {
            throw new NotImplementedException();
            /*	FILE *f;
                int i;

                f=fopen("screen.saw", "w");

                for(i=0;i<(VGLDisplay.Xsize*VGLDisplay.Ysize);i++)
                    fputc(VGLDisplay.Bitmap[i], f);
                fclose(f);*/
        }

        public static void sdl_enable_fullscreen()
        {
            addflag |= SDL_FULLSCREEN;
        }

        private static void DrawBackground(Bitmap bmp)
        {
            Surface tmp = null;
            vgageti(0, 0, ref tmp, 80, 200);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int lenght = bitmapData.Stride * bitmapData.Height;
            StdLib.MemCpy(tmp.pixels, bitmapData.Scan0, lenght);
            vgaputi(0, 0, tmp, 80, 200);
            tmp.Free();
        }

        // TODO: implement this
        public static void cgainit() { }

        public static void cgaclear() { }

        public static void cgatitle() { }

        public static void cgawrite(int x, int y, int ch, int c) { }

        public static void cgaputim(int x, int y, int ch, int w, int h) { }

        public static void cgageti(int x, int y, ref Surface tmp, int w, int h)
        {
            throw new NotImplementedException();
        }

        public static void cgaputi(int x, int y, Surface tmp, int w, int h) { }

        public static void cgapal(int pal) { }

        public static void cgainten(int inten) { }

        public static short cgagetpix(int x, int y) { return (0); }
    }
}