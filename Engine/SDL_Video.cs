using SDL2;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Digger.Net
{
    public enum VideoMode { CGA, VGA };
    public enum VideoIntensity { Normal, High }

    public class SDL_Video
    {
        private struct CharSurfacePlain
        {
            public byte[][] sprites;
            public Surface[] caches;
        };

        private IntPtr window;
        private IntPtr renderer;
        private IntPtr roottxt;
        private Surface screen;
        private Surface screen16;
        private SDL.SDL_Color[] palette1;
        private SDL.SDL_Color[] palette1i;
        private SDL.SDL_Color[] palette2;
        private SDL.SDL_Color[] palette2i;
        private CharSurfacePlain sprites;
        private CharSurfacePlain alphas;
        private SDL.SDL_Color[][] npalettes;
        private SDL.SDL_Color[][] ipalettes;
        private Bitmap backBitmap;
        private int screenRatio;

        private int curPalette = 0;
        private Func<byte, int, byte> GetPixelColorFromMask;

        public SDL_Video(VideoMode mode)
        {
            if (mode == VideoMode.CGA)
                InitializeCGA();
            else
                InitializeVGA();

            CreateCaches();
            CreateWindow();
            CreateGraphics();
            SetPalette(0);
        }

        public VideoMode VideoMode { get; private set; }

        private void CreateCaches()
        {
            alphas.caches = new Surface[256];
            sprites.caches = new Surface[256];
        }

        public void CreateWindow()
        {
            if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_VIDEO) < 0)
                throw new SystemException($"Couldn't initialize SDL: {SDL.SDL_GetError()}");

            window = SDL.SDL_CreateWindow("Digger", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 640, 400, 0);
            if (window == null)
                throw new SystemException($"SDL_CreateWindow() failed: {SDL.SDL_GetError()}");

            SDL.SDL_ShowCursor(1);
        }

        public void CreateGraphics()
        {
            renderer = SDL.SDL_CreateRenderer(window, -1, 0);
            if (renderer == null)
                throw new SystemException($"SDL_CreateRenderer() failed: {SDL.SDL_GetError()}");

            roottxt = SDL.SDL_CreateTexture(
                renderer,
                SDL.SDL_PIXELFORMAT_ARGB8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                Virtual2ScreenW(80),
                Virtual2ScreenH(200)
            );

            if (roottxt == null)
                throw new SystemException($"SDL_CreateTexture() failed: {SDL.SDL_GetError()}");

            screen = Surface.CreateRGBSurface(0, 640, 400, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
            if (screen == null)
                throw new SystemException($"SDL_CreateRGBSurface() failed: {SDL.SDL_GetError()}");

            screen16 = Surface.CreateRGBSurface(0, Virtual2ScreenW(80), Virtual2ScreenH(200), 8, 0, 0, 0, 0);
            if (screen16 == null)
                throw new SystemException($"SDL_CreateRGBSurface() failed: {SDL.SDL_GetError()}");
        }

        private SDL.SDL_Color[] CreatePaletteRGB(byte[,] colors)
        {
            var pallete = new SDL.SDL_Color[colors.GetLength(0)];
            for (int i = 0; i < colors.GetLength(0); ++i)
                pallete[i] = new SDL.SDL_Color()
                {
                    r = colors[i, 0],
                    g = colors[i, 1],
                    b = colors[i, 2],
                    a = colors[i, 3]
                };

            return pallete;
        }

        public void Clear()
        {
            SDL.SDL_FillRect(screen16, IntPtr.Zero, 0);
        }

        public void SetHighIntensity()
        {
            SetPallete(ipalettes[curPalette]);
        }

        public void SetNormalIntensity()
        {
            SetPallete(npalettes[curPalette]);
        }

        public void SetPalette(int palette)
        {
            SetPallete(npalettes[palette]);
            curPalette = palette;
        }

        public bool SetDisplayMode(bool isFullScreen)
        {
            if (isFullScreen)
                return SDL.SDL_SetWindowFullscreen(window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0;
            else
                return SDL.SDL_SetWindowFullscreen(window, 0) == 0;
        }

        public void UpdateScreen()
        {
            SDL.SDL_BlitSurface(screen16, IntPtr.Zero, screen, IntPtr.Zero);
            SDL.SDL_UpdateTexture(roottxt, IntPtr.Zero, screen.pixels, screen.pitch);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_RenderCopy(renderer, roottxt, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(renderer);
        }

        public bool TrySetVideoMode(VideoMode mode)
        {
            if (mode == VideoMode)
                return false;

            Clear();
            ReleaseCaches();
            FreeGraphics();

            if (mode == VideoMode.VGA)
                InitializeVGA();
            else
                InitializeCGA();

            CreateGraphics();
            CreateCaches();
            SetPalette(0);

            return true;
        }

        private void ReleaseCaches()
        {
            for (int i = 0; i < 256; i++)
            {
                alphas.caches[i]?.Free();
                sprites.caches[i]?.Free();
            }
        }

        public void FreeGraphics()
        {
            screen16.Free();
            screen.Free();
            SDL.SDL_DestroyTexture(roottxt);
            SDL.SDL_DestroyRenderer(renderer);
        }

        public void PutImage(int x, int y, Surface surface, int w, int h)
        {
            SDL.SDL_Rect rect = new SDL.SDL_Rect(
                Virtual2ScreenX(x),
                Virtual2ScreenY(y),
                Virtual2ScreenW(w),
                Virtual2ScreenH(h)
            );

            IntPtr reserv = surface.Format.palette;
            surface.SetSurfacePalette(screen16.Format.palette);
            surface.Blit(IntPtr.Zero, screen16, ref rect);
            surface.SetSurfacePalette(reserv);
        }

        /// <summary>
        /// Get image on given coordinates, with given size
        /// </summary>
        public void GetImage(int x, int y, ref Surface surface, int w, int h)
        {
            SDL.SDL_Rect src = new SDL.SDL_Rect(
                Virtual2ScreenX(x),
                Virtual2ScreenY(y),
                Virtual2ScreenW(w),
                Virtual2ScreenH(h)
            );

            if (surface == null)
            {
                surface = Surface.CreateRGBSurface(0, src.w, src.h, 8, 0, 0, 0, 0);
                SDL.SDL_SetSurfacePalette(surface, screen16.Format.palette);
            }

            screen16.Blit(ref src, surface, IntPtr.Zero);
        }

        public byte GetPixel(int x, int y)
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

        public void PutImage(int x, int y, int ch, int w, int h)
        {
            Surface tmp = Char2Surface(ref sprites, ch * 2, w, h);
            Surface mask = Char2Surface(ref sprites, ch * 2 + 1, w, h);

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

        public void WriteChar(int x, int y, int ch, int color)
        {
            int w = 3, h = 12;

            if (!Alpha.IsValidChar(ch))
                return;

            // Get char mask
            Surface tmp = Char2Surface(ref alphas, ch - 32, w, h);
            int size = tmp.w * tmp.h;
            byte[] pixels = new byte[size];
            Marshal.Copy(tmp.pixels, pixels, 0, pixels.Length);

            for (int i = 0; i < size; ++i)
                pixels[i] = GetPixelColorFromMask(pixels[i], color);

            // save original pixels
            IntPtr originalPixels = Marshal.AllocHGlobal(size);
            StdLib.MemCpy(originalPixels, tmp.pixels, size);

            Marshal.Copy(pixels, 0, tmp.pixels, size);
            PutImage(x, y, tmp, w, h);

            StdLib.MemCpy(tmp.pixels, originalPixels, size);
            Marshal.FreeHGlobal(originalPixels);
        }

        public void DrawTitleScreen()
        {
            Rectangle rect = new Rectangle(0, 0, backBitmap.Width, backBitmap.Height);
            Surface tmp = Surface.CreateRGBSurface(0, rect.Width, rect.Height, 8, 0, 0, 0, 0);
            tmp.SetSurfacePalette(screen16.Format.palette);

            var bitmapData = backBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int[] lookup = new int[backBitmap.Palette.Entries.Length];
            for (int i = 0; i < lookup.Length; i++)
                lookup[i] = GetColorIndex(palette1, backBitmap.Palette.Entries[i]);
            
            byte[] pixels = new byte[backBitmap.Width * backBitmap.Height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = (byte)lookup[Marshal.ReadByte(bitmapData.Scan0, i)];

            Marshal.Copy(pixels, 0, tmp.pixels, pixels.Length);
            backBitmap.UnlockBits(bitmapData);
            tmp.Blit(IntPtr.Zero, screen16, IntPtr.Zero);
            tmp.Free();
        }

        private int GetColorIndex(SDL.SDL_Color[] palette, Color color)
        {
            int rgbColor = color.ToArgb() & 0xFFFFFF; // remove alpha channel
            for (int i = 0; i < palette.Length; i++)
                if (palette[i].ToArgb() == rgbColor)
                    return i;

            return 0;
        }

        private void InitializeVGA()
        {
            palette1 = CreatePaletteRGB(VgaGrafx.Palette1);
            palette1i = CreatePaletteRGB(VgaGrafx.Palette1i);
            palette2 = CreatePaletteRGB(VgaGrafx.Palette2);
            palette2i = CreatePaletteRGB(VgaGrafx.Palette2i);

            npalettes = new[] { palette1, palette2 };
            ipalettes = new[] { palette1i, palette2i };

            alphas.sprites = DecompressAlpha(Alpha.ascii2vga);
            sprites.sprites = VgaGrafx.SpriteTable;

            GetPixelColorFromMask = GetPixelColorFromMaskVga;
            backBitmap = Properties.Resource.vtitle;
            VideoMode = VideoMode.VGA;
            screenRatio = 2;
        }

        private void InitializeCGA()
        {
            palette1 = CreatePaletteRGB(CgaGrafx.Palette1);
            palette1i = CreatePaletteRGB(CgaGrafx.Pallette1i);
            palette2 = CreatePaletteRGB(CgaGrafx.Paletter2);
            palette2i = CreatePaletteRGB(CgaGrafx.Palette2i);

            npalettes = new[] { palette1, palette2 };
            ipalettes = new[] { palette1i, palette2i };

            alphas.sprites = DecompressAlpha(Alpha.ascii2cga);
            sprites.sprites = DecompressSprites(CgaGrafx.SpriteTable);

            GetPixelColorFromMask = GetPixelColorFromMaskCga;
            backBitmap = Properties.Resource.ctitle;
            VideoMode = VideoMode.CGA;
            screenRatio = 1;
        }

        private byte[][] DecompressAlpha(byte[][] table)
        {
            byte[][] expandedTable = new byte[table.Length][];
            for (int i = 0; i < table.Length; i++)
                expandedTable[i] = ExpandBytes(table[i]);

            return expandedTable;
        }

        private byte[][] DecompressSprites(byte[][] table)
        {
            byte[][] expandedTable = new byte[table.Length][];
            for (int i = 0; i < table.Length; i++)
            {
                expandedTable[i] = ExpandBytes(table[i]);
                expandedTable[i] = ExpandNibbles(expandedTable[i]);
            }

            return expandedTable;
        }

        private byte[] ExpandBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;

            var expanded = new byte[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte hi = (byte)(bytes[i] >> 4);
                byte lo = (byte)(bytes[i] & 0x0F);
                expanded[2 * i + 0] = hi;
                expanded[2 * i + 1] = lo;
            }

            return expanded;
        }

        private byte[] ExpandNibbles(byte[] bytes)
        {
            if (bytes == null)
                return null;

            var expanded = new byte[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte hi = (byte)(bytes[i] >> 2);
                byte lo = (byte)(bytes[i] & 0x03);
                expanded[2 * i + 0] = hi;
                expanded[2 * i + 1] = lo;
            }

            return expanded;
        }

        private Surface Char2Surface(ref CharSurfacePlain planep, int sprite, int w, int h)
        {
            if (planep.caches[sprite] != null)
                return (planep.caches[sprite]);

            int realw = Virtual2ScreenW(w);
            int realh = Virtual2ScreenH(h);
            int size = realw * realh;
            IntPtr pixels = Marshal.AllocHGlobal(size);
            Marshal.Copy(planep.sprites[sprite], 0, pixels, size);
            Surface surface = Surface.CreateRGBSurfaceFrom(pixels, realw, realh, 8, realw, 0, 0, 0, 0);
            SDL.SDL_SetPaletteColors(surface.Format.palette, npalettes[0], 0, 16);
            planep.caches[sprite] = surface;

            return surface;
        }

        private void SetPallete(SDL.SDL_Color[] pal)
        {
            SDL.SDL_SetPaletteColors(screen16.Format.palette, pal, 0, 16);
        }

        private byte GetPixelColorFromMaskVga(byte mask, int color)
        {
            if (mask == 10)
            {
                if (color == 2)
                    return 12;

                if (color == 3)
                    return 14;
            }
            else if (mask == 12)
            {
                if (color == 1)
                    return 2;

                if (color == 2)
                    return 4;

                if (color == 3)
                    return 6;
            }

            return mask;
        }

        private byte GetPixelColorFromMaskCga(byte mask, int color)
        {
            return (byte)(mask & color);
        }

        private int Virtual2ScreenX(int x) => x * screenRatio;
        private int Virtual2ScreenY(int y) => y * screenRatio;
        private int Virtual2ScreenW(int w) => w * screenRatio * 4;
        private int Virtual2ScreenH(int h) => h * screenRatio;
    }
}