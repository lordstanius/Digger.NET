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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SDL2;

namespace Digger.Net
{
    public class SdlKeyboard
    {
        private static SDL.SDL_EventFilter pHandler;

        public SdlKeyboard()
        {
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_MOUSEMOTION, SDL.SDL_IGNORE);
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN, SDL.SDL_IGNORE);
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_MOUSEBUTTONUP, SDL.SDL_IGNORE);

            pHandler = new SDL.SDL_EventFilter(Handler);
            SDL.SDL_SetEventFilter(pHandler, IntPtr.Zero);
        }

        public bool IsRightPressed => GetAsyncKeyState(keycodes[0][0]);
        public bool IsUpPressed => GetAsyncKeyState(keycodes[1][0]);
        public bool IsLeftPressed => GetAsyncKeyState(keycodes[2][0]);
        public bool IsDownPressed => GetAsyncKeyState(keycodes[3][0]);
        public bool IsF1Pressed => GetAsyncKeyState(keycodes[4][0]);
        public bool IsRight2Pressed => GetAsyncKeyState(keycodes[5][0]);
        public bool IsUp2Pressed => GetAsyncKeyState(keycodes[6][0]);
        public bool IsLeft2Pressed => GetAsyncKeyState(keycodes[7][0]);
        public bool IsDown2Pressed => GetAsyncKeyState(keycodes[8][0]);
        public bool IsF12Pressed => GetAsyncKeyState(keycodes[9][0]);

        public const int KBLEN = 30;

        public readonly int[][] keycodes = {
            new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_RIGHT,-2,-2,-2,-2},    /* 1 Right */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_UP,-2,-2,-2,-2},       /* 1 Up */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_LEFT,-2,-2,-2,-2},     /* 1 Left */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_DOWN,-2,-2,-2,-2},     /* 1 Down */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_F1,-2,-2,-2,-2},       /* 1 Fire */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_S,-2,-2,-2,-2},        /* 2 Right */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_W,-2,-2,-2,-2},        /* 2 Up */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_A,-2,-2,-2,-2},        /* 2 Left */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_Z,-2,-2,-2,-2},        /* 2 Down */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_TAB,-2,-2,-2,-2},      /* 2 Fire */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_T,-2,-2,-2,-2},        /* Cheat */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS,-2,-2,-2,-2},  /* Accelerate */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS,-2,-2,-2,-2}, /* Brake */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_F7,-2,-2,-2,-2},       /* Music */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_F9,-2,-2,-2,-2},       /* Sound */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_F10,-2,-2,-2,-2},      /* Exit */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_SPACE,-2,-2,-2,-2},    /* Pause */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_N,-2,-2,-2,-2},        /* Change mode */
			new int[]{(int)SDL.SDL_Scancode.SDL_SCANCODE_F8,-2,-2,-2,-2}};      /* Save DRF */

        public struct kbent
        {
            public SDL.SDL_Keycode sym;
            public SDL.SDL_Scancode scancode;
        };

        static kbent[] kbuffer = new kbent[KBLEN];
        public static short klen = 0;

        public int Handler(IntPtr udata, IntPtr pEvent)
        {
            SDL.SDL_Event sdlEvent = pEvent.ToStruct<SDL.SDL_Event>();
            if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
            {
                if (klen == KBLEN)
                {
                    /* Buffer is full, drop some pieces */
                    klen--;
                    ShiftLeft(kbuffer);
                }
                kbuffer[klen].scancode = sdlEvent.key.keysym.scancode;
                kbuffer[klen].sym = sdlEvent.key.keysym.sym;
                klen++;

                /* ALT + Enter handling (fullscreen/windowed operation) */
                if ((sdlEvent.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_RETURN ||
                    sdlEvent.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER) &&
                    ((sdlEvent.key.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0))
                {
                    DiggerC.gfx.SwitchMode();
                }
            }

            if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                Environment.Exit(0);

            return 1;
        }

        public bool GetAsyncKeyState(int key)
        {
            SDL.SDL_PumpEvents();
            IntPtr pKeys = SDL.SDL_GetKeyboardState(out int numkeys);
            string keys = Marshal.PtrToStringAnsi(pKeys, numkeys);
            return keys[key] == SDL.SDL_PRESSED;
        }

        public int GetKey(bool scancode)
        {
            int result;

            while (!IsKeyboardHit())
            {
                DiggerC.timer.SyncFrame();
                DiggerC.gfx.UpdateScreen();
            }

            if (scancode)
            {
                result = (int)kbuffer[0].scancode;
            }
            else
            {
                result = (int)kbuffer[0].sym;
            }
            klen--;
            ShiftLeft(kbuffer);

            return result;
        }

        public bool IsKeyboardHit()
        {
            SDL.SDL_PumpEvents();

            return klen > 0;
        }

        private static void ShiftLeft<T>(IList<T> list)
        {
            for (int i = 1; i < list.Count; ++i)
                list[i - 1] = list[i];
        }
    }
}