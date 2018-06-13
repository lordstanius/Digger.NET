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
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SDL2;

namespace Digger.Source
{
    public class SDL_Input
    {
        public const int KBLEN = 30;

        private readonly SDL.SDL_Scancode[] defaultKeys =
        {
            SDL.SDL_Scancode.SDL_SCANCODE_RIGHT,        /* 1 Right */
            SDL.SDL_Scancode.SDL_SCANCODE_UP,           /* 1 Up */
            SDL.SDL_Scancode.SDL_SCANCODE_LEFT,         /* 1 Left */
            SDL.SDL_Scancode.SDL_SCANCODE_DOWN,         /* 1 Down */
            SDL.SDL_Scancode.SDL_SCANCODE_F1,           /* 1 Fire */
            SDL.SDL_Scancode.SDL_SCANCODE_D,            /* 2 Right */
            SDL.SDL_Scancode.SDL_SCANCODE_W,            /* 2 Up */
            SDL.SDL_Scancode.SDL_SCANCODE_A,            /* 2 Left */
            SDL.SDL_Scancode.SDL_SCANCODE_S,            /* 2 Down */
            SDL.SDL_Scancode.SDL_SCANCODE_TAB,          /* 2 Fire */
            SDL.SDL_Scancode.SDL_SCANCODE_T,            /* Cheat */
            SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS,      /* Accelerate */
            SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS,     /* Brake */
            SDL.SDL_Scancode.SDL_SCANCODE_F7,           /* Music */
            SDL.SDL_Scancode.SDL_SCANCODE_F9,           /* Sound */
            SDL.SDL_Scancode.SDL_SCANCODE_F10,          /* Exit */
            SDL.SDL_Scancode.SDL_SCANCODE_SPACE,        /* Pause */
            SDL.SDL_Scancode.SDL_SCANCODE_N,            /* Change mode */
            SDL.SDL_Scancode.SDL_SCANCODE_F8,           /* Save DRF */
            SDL.SDL_Scancode.SDL_SCANCODE_V,            /* Change video to VGA */
            SDL.SDL_Scancode.SDL_SCANCODE_C,            /* Change video to CGA */
        };

        public readonly int[][] keycodes;
        public static short klen = 0;
        private static SDL.SDL_EventFilter pHandler;
        private readonly KeyBufferEntry[] kbuffer = new KeyBufferEntry[KBLEN];

        private Game game;

        public SDL_Input(Game game)
        {
            this.game = game;

            SDL.SDL_EventState(SDL.SDL_EventType.SDL_MOUSEMOTION, SDL.SDL_IGNORE);
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN, SDL.SDL_IGNORE);
            SDL.SDL_EventState(SDL.SDL_EventType.SDL_MOUSEBUTTONUP, SDL.SDL_IGNORE);

            pHandler = new SDL.SDL_EventFilter(Handler);
            SDL.SDL_SetEventFilter(pHandler, IntPtr.Zero);

            keycodes = new int[defaultKeys.Length][];
            for (int i = 0; i < defaultKeys.Length; ++i)
                keycodes[i] = new []{ (int)defaultKeys[i], -2, -2, -2, -2 };
        }

        public int KeyCount => defaultKeys.Length;
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

        public struct KeyBufferEntry
        {
            public SDL.SDL_Keycode sym;
            public SDL.SDL_Scancode scancode;
        };

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
                    game.gfx.SwitchFullscreenWindow();
                }
            }

            if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
            {
                game.isGameCycleEnded = true;
                game.shouldExit = true;
            }

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
            while (!IsKeyboardHit() && !game.shouldExit)
                game.timer.SyncFrame();

            int result = scancode ?
                (int)kbuffer[0].scancode :
                (int)kbuffer[0].sym;

            klen--;
            ShiftLeft(kbuffer);

            return result;
        }

        public bool IsKeyboardHit()
        {
            SDL.SDL_PumpEvents();

            return klen > 0;
        }

        public bool IsKeyRemapped(int index)
        {
            return keycodes[index][0] != (int)defaultKeys[index];
        }

        private static void ShiftLeft<T>(IList<T> list)
        {
            for (int i = 1; i < list.Count; ++i)
                list[i - 1] = list[i];
        }
    }
}