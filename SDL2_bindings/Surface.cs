// Surface wrapper

using System;
using System.Runtime.InteropServices;

namespace SDL2
{
    public class Surface
    {
        private IntPtr pSurface;
        private SDL.SDL_Surface _surface;

        internal Surface(IntPtr surface)
        {
            pSurface = surface;
            this._surface = pSurface.ToStruct<SDL.SDL_Surface>();
        }

        public static Surface FromFile(string filePath)
        {
            return new Surface(SDL.SDL_LoadBMP(filePath));
        }

        public static implicit operator IntPtr(Surface surface)
        {
            if (surface == null)
                return IntPtr.Zero;

            return surface.pSurface;
        }

        public uint flags
        {
            get { return _surface.flags; }            
        }

        public IntPtr format => _surface.format;

        public SDL.SDL_PixelFormat Format
        {
            get => _surface.format.ToStruct<SDL.SDL_PixelFormat>();
            set => Marshal.StructureToPtr(value, _surface.format, true);
        }

        public int w => _surface.w;

        public int h => _surface.h;

        public int pitch => _surface.pitch;

        public IntPtr pixels // void*
        {
            get => _surface.pixels;
            set => _surface.pixels = value;
        }

        public IntPtr userdata
        {
            get => _surface.userdata;
            set => _surface.userdata = value;
        }

        public int locked => _surface.locked;

        public IntPtr lock_data => _surface.lock_data;

        public SDL.SDL_Rect ClipRect
        {
            get
            {
                SDL.SDL_GetClipRect(pSurface, out SDL.SDL_Rect rect);
                return rect;
            }
            set
            {
                if (SDL.SDL_SetClipRect(pSurface, ref value) != SDL.SDL_bool.SDL_TRUE)
                    throw new InvalidOperationException($"{nameof(SDL.SDL_SetClipRect)} failed.");
            }
        }

        public IntPtr map // BlitMap*
        {
            get { return _surface.map; }
            set { _surface.map = value; }
        }

        public int refcount
        {
            get { return _surface.refcount; }
        }

        public bool MUSTLOCK => (_surface.flags & SDL.SDL_RLEACCEL) != 0;

        public int Blit(ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_BlitSurface(pSurface, ref srcrect, dst, ref dstrect);
        }

        /* Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
        public int Blit(IntPtr srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_BlitSurface(pSurface, srcrect, dst, ref dstrect);
        }

        /* src and dst refer to an Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
        public int Blit(ref SDL.SDL_Rect srcrect, Surface dst, IntPtr dstrect)
        {
            return SDL.SDL_BlitSurface(pSurface, ref srcrect, dst, dstrect);
        }

        /* src and dst refer to an Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both Rects.
		 */
        public int Blit(IntPtr srcrect, Surface dst, IntPtr dstrect)
        {
            return SDL.SDL_BlitSurface(pSurface, srcrect, dst, dstrect);
        }

        /* src and dst refer to an Surface* */
        public int BlitScaled(ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_BlitScaled(pSurface, ref srcrect, dst, ref dstrect);
        }

        /* src and dst refer to an Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
        public int BlitScaled(IntPtr srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_BlitScaled(pSurface, srcrect, dst, ref dstrect);
        }

        /* src and dst refer to an Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
        public int BlitScaled(ref SDL.SDL_Rect srcrect, Surface dst, IntPtr dstrect)
        {
            return SDL.SDL_BlitScaled(pSurface, ref srcrect, dst, dstrect);
        }

        /* src and dst refer to an Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both Rects.
		 */
        public int BlitScaled(IntPtr srcrect, Surface dst, IntPtr dstrect)
        {
            return SDL.SDL_BlitScaled(pSurface, srcrect, dst, dstrect);
        }

        /* src and dst are void* pointers */
        public int ConvertPixels(int width, int height, uint src_format, int src_pitch, uint dst_format, Surface dst, int dst_pitch)
        {
            return SDL.SDL_ConvertPixels(width, height, src_format, pSurface, src_pitch, dst_format, dst, dst_pitch);
        }

        /* IntPtr refers to an Surface*
		 * src refers to an Surface*
		 * fmt refers to an PixelFormat*
		 */
        public Surface ConvertSurface(IntPtr fmt, uint flags)
        {
            return new Surface(SDL.SDL_ConvertSurface(pSurface, fmt, flags));
        }

        /* IntPtr refers to an Surface*, src to an Surface* */
        public Surface ConvertSurfaceFormat(uint pixel_format, uint flags)
        {
            return new Surface(SDL.SDL_ConvertSurfaceFormat(pSurface, pixel_format, flags));
        }

        /* IntPtr refers to an Surface* */
        public static Surface CreateRGBSurface(uint flags, int width, int height, int depth, uint Rmask, uint Gmask, uint Bmask, uint Amask)
        {
            return new Surface(SDL.SDL_CreateRGBSurface(flags, width, height, depth, Rmask, Gmask, Bmask, Amask));
        }

        /* IntPtr refers to an Surface*, pixels to a void* */
        public static Surface CreateRGBSurfaceFrom(IntPtr pixels, int width, int height, int depth, int pitch, uint Rmask, uint Gmask, uint Bmask, uint Amask)
        {
            return new Surface(SDL.SDL_CreateRGBSurfaceFrom(pixels, width, height, depth, pitch, Rmask, Gmask, Bmask, Amask));
        }

        /* Available in 2.0.5 or higher */
        public static Surface CreateRGBSurfaceWithFormat(uint flags, int width, int height, int depth, uint format)
        {
            return new Surface(SDL.SDL_CreateRGBSurfaceWithFormat(flags, width, height, depth, format));
        }

        /* IntPtr refers to an Surface*, pixels to a void* */
        /* Available in 2.0.5 or higher */
        public Surface CreateRGBSurfaceWithFormatFrom(IntPtr pixels, int width, int height, int depth, int pitch, uint format)
        {
            return new Surface(SDL.SDL_CreateRGBSurfaceWithFormatFrom(pixels, width, height, depth, pitch, format));
        }

        /* dst refers to an Surface* */
        public int FillRect(ref SDL.SDL_Rect rect, uint color)
        {
            return SDL.SDL_FillRect(pSurface, ref rect, color);
        }

        /* dst refers to an Surface*.
		 * This overload allows passing NULL to rect.
		 */
        public int FillRect(IntPtr rect, uint color)
        {
            return SDL.SDL_FillRect(pSurface, rect, color);
        }

        /* dst refers to an Surface* */
        public int FillRects(SDL.SDL_Rect[] rects, uint color)
        {
            return SDL.SDL_FillRects(pSurface, rects, rects.Length, color);
        }

        public void Free()
        {
            SDL.SDL_FreeSurface(pSurface);
        }

        /* surface refers to an Surface* */
        public int GetColorKey(out uint key)
        {
            return SDL.SDL_GetColorKey(pSurface, out key);
        }

        public int GetSurfaceAlphaMod(out byte alpha)
        {
            return SDL.SDL_GetSurfaceAlphaMod(pSurface, out alpha);
        }

        /* surface refers to an Surface* */
        public int GetSurfaceBlendMode(out SDL.SDL_BlendMode blendMode)
        {
            return SDL.SDL_GetSurfaceBlendMode(pSurface, out blendMode);
        }

        /* surface refers to an Surface* */
        public int GetSurfaceColorMod(out byte r, out byte g, out byte b)
        {
            return SDL.SDL_GetSurfaceColorMod(pSurface, out r, out g, out b);
        }

        public static Surface LoadBMP(string file)
        {
            return new Surface(SDL.SDL_LoadBMP(file));
        }

        public int LockSurface()
        {
            return SDL.SDL_LockSurface(pSurface);
        }

        /* src and dst refer to an Surface* */
        public int LowerBlit(ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_LowerBlit(pSurface, ref srcrect, dst, ref dstrect);
        }

        /* src and dst refer to an Surface* */
        public int LowerBlitScaled(IntPtr src, ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_LowerBlitScaled(pSurface, ref srcrect, dst, ref dstrect);
        }

        public int SaveBMP(string file)
        {
            return SDL.SDL_SaveBMP(pSurface, file);
        }

        /* surface refers to an Surface* */
        public int SetColorKey(int flag, uint key)
        {
            return SDL.SDL_SetColorKey(pSurface, flag, key);
        }

        public int SetSurfaceAlphaMod(byte alpha)
        {
            return SDL.SDL_SetSurfaceAlphaMod(pSurface, alpha);
        }

        public int SetSurfaceBlendMode(SDL.SDL_BlendMode blendMode)
        {
            return SDL.SDL_SetSurfaceBlendMode(pSurface, blendMode);
        }

        public int SetSurfaceColorMod(byte r, byte g, byte b)
        {
            return SDL.SDL_SetSurfaceColorMod(pSurface, r, g, b);
        }

        /* surface refers to an Surface*, palette to an Palette* */
        public int SetSurfacePalette(IntPtr palette)
        {
            return SDL.SDL_SetSurfacePalette(pSurface, palette);
        }

        /* surface refers to an Surface* */
        public int SetSurfaceRLE(int flag)
        {
            return SDL.SDL_SetSurfaceRLE(pSurface, flag);
        }

        public int SoftStretch(ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_SoftStretch(pSurface, ref srcrect, dst, ref dstrect);
        }

        public void Unlock()
        {
            SDL.SDL_UnlockSurface(pSurface);
        }

        public int UpperBlit(ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_UpperBlit(pSurface, ref srcrect, dst, ref dstrect);
        }

        public int UpperBlitScaled(ref SDL.SDL_Rect srcrect, Surface dst, ref SDL.SDL_Rect dstrect)
        {
            return SDL.SDL_UpperBlitScaled(pSurface, ref srcrect, dst, ref dstrect);
        }

        public Surface Duplicate()
        {
            return new Surface(SDL.SDL_DuplicateSurface(pSurface));
        }
    }
}
