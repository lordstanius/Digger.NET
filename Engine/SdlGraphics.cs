using SDL2;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Digger.Net
{
    public abstract class SdlGraphics
    {
        protected struct Char2Surface
        {
            public Char2Surface(byte[][] table)
            {
                caches = new Surface[256];
                sprites = table;
            }

            public byte[][] sprites;
            public Surface[] caches;
        };

        protected IntPtr window;
        protected IntPtr renderer;
        protected IntPtr roottxt;
        protected Surface screen;
        protected Surface screen16;

        protected SDL.SDL_Color[] pal1;
        protected SDL.SDL_Color[] pal1i;
        protected SDL.SDL_Color[] pal2;
        protected SDL.SDL_Color[] pal2i;

        protected Char2Surface sprites;
        protected Char2Surface alphas;
        protected SDL.SDL_Color[][] npalettes;
        protected SDL.SDL_Color[][] ipalettes;

        private bool isFullScreen;
        private int curPalette = 0;

        public abstract void PutImage(int x, int y, Surface surface, int w, int h);
        public abstract void GetImage(int x, int y, ref Surface tmp, int w, int h);
        public abstract void PutImage(int x, int y, int ch, int w, int h);
        public abstract void DrawTitleScreen();
        public abstract void WriteChar(int x, int y, int ch, int c);
        public abstract byte GetPixel(int x, int y);

        public void Initialize()
        {
            if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_VIDEO) < 0)
                throw new SystemException($"Couldn't initialize SDL: {SDL.SDL_GetError()}");

            window = SDL.SDL_CreateWindow("Digger", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 640, 400, 0);
            if (window == null)
                throw new SystemException($"SDL_CreateWindow() failed: {SDL.SDL_GetError()}");

            renderer = SDL.SDL_CreateRenderer(window, -1, 0);
            if (renderer == null)
                throw new SystemException($"SDL_CreateRenderer() failed: {SDL.SDL_GetError()}");

            roottxt = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 640, 400);
            if (roottxt == null)
                throw new SystemException($"SDL_CreateTexture() failed: {SDL.SDL_GetError()}");

            screen = Surface.CreateRGBSurface(0, 640, 400, 32, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
            if (screen == null)
                throw new SystemException($"SDL_CreateRGBSurface() failed: {SDL.SDL_GetError()}");

            screen16 = Surface.CreateRGBSurface(0, 640, 400, 8, 0, 0, 0, 0);
            if (screen16 == null)
                throw new SystemException($"SDL_CreateRGBSurface() failed: {SDL.SDL_GetError()}");

            if (!SetDisplayMode())
                throw new SystemException($"Couldn't set 640x400x8 video mode: {SDL.SDL_GetError()}");

            SDL.SDL_ShowCursor(1);
        }

        public void Clear()
        {
            Surface tmp = null;
            GetImage(0, 0, ref tmp, 80, 200);
            byte[] empty = new byte[tmp.w * tmp.h];
            Marshal.Copy(empty, 0, tmp.pixels, empty.Length);
            PutImage(0, 0, tmp, 80, 200);
            tmp.Free();
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

        public virtual void SwitchDisplayMode()
        {
            isFullScreen = !isFullScreen;
            if (!SetDisplayMode())
            {
                isFullScreen = !isFullScreen;
                DebugLog.Write($"Fatal: failed to change videomode: {SDL.SDL_GetError()}");
            }
        }

        public virtual void EnableFullScreen()
        {
            isFullScreen = true;
        }

        public virtual void UpdateScreen()
        {
            screen16.Blit(IntPtr.Zero, screen, IntPtr.Zero);
            SDL.SDL_UpdateTexture(roottxt, IntPtr.Zero, screen.pixels, screen.pitch);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_RenderCopy(renderer, roottxt, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(renderer);
        }

        public virtual bool SetDisplayMode()
        {
            if (isFullScreen)
                return SDL.SDL_SetWindowFullscreen(window, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) == 0;
            else
                return SDL.SDL_SetWindowFullscreen(window, 0) == 0;
        }

        protected void DrawBackground(Bitmap bmp)
        {
            Surface tmp = null;
            GetImage(0, 0, ref tmp, 80, 200);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            int lenght = bitmapData.Stride * bitmapData.Height;
            StdLib.MemCpy(tmp.pixels, bitmapData.Scan0, lenght);
            PutImage(0, 0, tmp, 80, 200);
            tmp.Free();
        }

        protected SDL.SDL_Color[] CreatePalette(byte[,] colors)
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

        private void SetPallete(SDL.SDL_Color[] pal)
        {
            SDL.SDL_SetPaletteColors(screen16.Format.palette, pal, 0, 16);
        }
    }
}