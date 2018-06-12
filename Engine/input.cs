/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

namespace Digger.Net
{
    public class Input
    {
        private const int KEY_CHEAT = 10;         /* Cheat */
        private const int KEY_SPEED_UP = 11;      /* Increase speed */
        private const int KEY_SPEED_DOWN = 12;    /* Decrease speed */
        private const int KEY_MUSIC_TOGGLE = 13;  /* Toggle music */
        private const int KEY_SOUND_TOGGLE = 14;  /* Toggle sound */
        private const int KEY_EXIT = 15;          /* Exit */
        private const int KEY_PAUSE = 16;         /* Pause */
        private const int KEY_MODE_CHANGE = 17;   /* Mode change */
        private const int KEY_SAVE_DRF = 18;      /* Save DRF */
        private const int KEY_SWITCH_TO_VGA = 19; // Change video to VGA
        private const int KEY_SWITCH_TO_CGA = 20; // Change video to CGA

        public bool firepflag;
        public bool fire2pflag;
        public bool pausef;
        public bool mode_change;

        private bool isLeftPressed;
        private bool isRightPressed;
        private bool isUpPressed;
        private bool isDownPressed;
        private bool shouldStart;
        private bool isF1Pressed;
        private bool isLeft2Pressed;
        private bool isRight2Pressed;
        private bool isUp2Pressed;
        private bool isDown2Pressed;
        private bool isF12Pressed;
        private bool oupressed, odpressed, olpressed, orpressed;
        private bool ou2pressed, od2pressed, ol2pressed, or2pressed;

        private int dynamicdir = -1, dynamicdir2 = -1, dir = -1, dir2 = -1;
        private int keyPressed;
        private int keydir, keydir2;

        private readonly Game game;
        private readonly SDL_Input keyboard;

        public Input(Game game)
        {
            this.game = game;
            this.keyboard = new SDL_Input(game);
        }

        public int[][] KeyCodes => keyboard.keycodes;
        public int KeyCount => keyboard.KeyCount;
        public bool IsKeyboardHit => keyboard.IsKeyboardHit();

        public int ProcessKey(int keyNo)
        {
            int key = keyboard.GetKey(true);
            if (keyNo != KEY_EXIT && key == keyboard.keycodes[KEY_EXIT][0])
                return -1;

            keyboard.keycodes[keyNo][0] = key;
            return (0);
        }

        /* The standard ASCII keyboard is also checked so that very short keypresses
           are not overlooked. The functions kbhit() (returns bool denoting whether or
           not there is a key in the buffer) and getkey() (wait until a key is in the
           buffer, then return it) are used. These functions are emulated on platforms
           which only provide an inkey() function (return the key in the buffer, unless
           there is none, in which case return -1. It is done this way around for
           historical reasons, there is no fundamental reason why it shouldn't be the
           other way around. */
        public void CheckKeyBuffer()
        {
            if (keyboard.IsLeftPressed)
                isLeftPressed = true;

            if (keyboard.IsRightPressed)
                isRightPressed = true;

            if (keyboard.IsUpPressed)
                isUpPressed = true;

            if (keyboard.IsDownPressed)
                isDownPressed = true;

            if (keyboard.IsF1Pressed)
                isF1Pressed = true;

            if (keyboard.IsLeft2Pressed)
                isLeft2Pressed = true;

            if (keyboard.IsRight2Pressed)
                isRight2Pressed = true;

            if (keyboard.IsUp2Pressed)
                isUp2Pressed = true;

            if (keyboard.IsDown2Pressed)
                isDown2Pressed = true;

            if (keyboard.IsF12Pressed)
                isF12Pressed = true;

            int k = 0;
            while (keyboard.IsKeyboardHit())
            {
                keyPressed = keyboard.GetKey(true);
                for (int i = 0; i < 10; i++)
                    for (int j = 2; j < 5; j++)
                        if (keyPressed == keyboard.keycodes[i][j])
                            AsyncFlagPressed(i, true);

                for (int i = 10; i < KeyCount; i++)
                    for (int j = 0; j < 5; j++)
                        if (keyPressed == keyboard.keycodes[i][j])
                            k = i;

                switch (k)
                {
                    case KEY_CHEAT: /* Cheat! */
                        if (!game.isGauntletMode)
                        {
                            game.record.IsPlaying = false;
                            game.record.IsDrfValid = false;
                            game.hasUnlimitedLives = true;
                        }
                        break;
                    case KEY_SPEED_UP: /* Increase speed */
                        if (game.timer.FrameTime > 10000)
                            game.timer.FrameTime -= 10000;
                        break;
                    case KEY_SPEED_DOWN: /* Decrease speed */
                        game.timer.FrameTime += 10000;
                        break;
                    case KEY_MUSIC_TOGGLE: /* Toggle music */
                        game.sound.isMusicEnabled = !game.sound.isMusicEnabled;
                        break;
                    case KEY_SOUND_TOGGLE: /* Toggle sound */
                        game.sound.isSoundEnabled = !game.sound.isSoundEnabled;
                        break;
                    case KEY_EXIT: /* Exit */
                        game.isGameCycleEnded = true;
                        break;
                    case KEY_PAUSE: /* Pause */
                        pausef = true;
                        break;
                    case KEY_MODE_CHANGE: /* Mode change */
                        mode_change = true;
                        break;
                    case KEY_SAVE_DRF: /* Save DRF */
                        game.record.SaveDrf = true;
                        break;
                    case KEY_SWITCH_TO_VGA:
                        if (!game.record.IsPlaying && !game.isStarted)
                            game.video.SetVideoMode(VideoMode.VGA);
                        break;
                    case KEY_SWITCH_TO_CGA:
                        if (!game.record.IsPlaying && !game.isStarted)
                            game.video.SetVideoMode(VideoMode.CGA);
                        break;
                }

                if (!mode_change)
                    shouldStart = true;                                /* Change number of players */
            }
        }

        private void AsyncFlagPressed(int i, bool value)
        {
            switch (i)
            {
                case 0: isRightPressed = value; break;
                case 1: isUpPressed = value; break;
                case 2: isLeftPressed = value; break;
                case 3: isDownPressed = value; break;
                case 4: isF1Pressed = value; break;
                case 5: isRight2Pressed = value; break;
                case 6: isUp2Pressed = value; break;
                case 7: isLeft2Pressed = value; break;
                case 8: isDown2Pressed = value; break;
                case 9: isF12Pressed = value; break;
            }
        }

        /* Contrary to some beliefs, you don't need a separate OS call to flush the
           keyboard buffer. */
        public void FlushKeyBuffer()
        {
            while (keyboard.IsKeyboardHit())
                keyboard.GetKey(true);

            isLeftPressed = isRightPressed = isUpPressed = isDownPressed = isF1Pressed = false;
            isLeft2Pressed = isRight2Pressed = isUp2Pressed = isDown2Pressed = isF12Pressed = false;
        }

        public void ClearFire(int n)
        {
            if (n == 0)
                isF1Pressed = false;
            else
                isF12Pressed = false;
        }

        public void ReadDirection(int n)
        {
            bool u = false, d = false, l = false, r = false;
            bool u2 = false, d2 = false, l2 = false, r2 = false;

            if (n == 0)
            {
                if (isUpPressed || keyboard.IsUpPressed) { u = true; isUpPressed = false; }
                if (isDownPressed || keyboard.IsDownPressed) { d = true; isDownPressed = false; }
                if (isLeftPressed || keyboard.IsLeftPressed) { l = true; isLeftPressed = false; }
                if (isRightPressed || keyboard.IsRightPressed) { r = true; isRightPressed = false; }
                if (keyboard.IsF1Pressed || isF1Pressed)
                {
                    firepflag = true;
                    isF1Pressed = false;
                }
                else
                    firepflag = false;
                if (u && !oupressed)
                    dir = dynamicdir = Dir.Up;
                if (d && !odpressed)
                    dir = dynamicdir = Dir.Down;
                if (l && !olpressed)
                    dir = dynamicdir = Dir.Left;
                if (r && !orpressed)
                    dir = dynamicdir = Dir.Right;
                if ((oupressed && !u && dynamicdir == Dir.Up) ||
                    (odpressed && !d && dynamicdir == Dir.Down) ||
                    (olpressed && !l && dynamicdir == Dir.Left) ||
                    (orpressed && !r && dynamicdir == Dir.Right))
                {
                    dynamicdir = Dir.None;
                    if (u) dynamicdir = dir = 2;
                    if (d) dynamicdir = dir = 6;
                    if (l) dynamicdir = dir = 4;
                    if (r) dynamicdir = dir = 0;
                }
                oupressed = u;
                odpressed = d;
                olpressed = l;
                orpressed = r;
                keydir = dir;
                if (dynamicdir != Dir.None)
                    keydir = dynamicdir;
                dir = Dir.None;
            }
            else
            {
                if (isUp2Pressed || keyboard.IsUp2Pressed) { u2 = true; isUp2Pressed = false; }
                if (isDown2Pressed || keyboard.IsDown2Pressed) { d2 = true; isDown2Pressed = false; }
                if (isLeft2Pressed || keyboard.IsLeft2Pressed) { l2 = true; isLeft2Pressed = false; }
                if (isRight2Pressed || keyboard.IsRight2Pressed) { r2 = true; isRight2Pressed = false; }
                if (keyboard.IsF12Pressed || isF12Pressed)
                {
                    fire2pflag = true;
                    isF12Pressed = false;
                }
                else
                    fire2pflag = false;
                if (u2 && !ou2pressed)
                    dir2 = dynamicdir2 = Dir.Up;
                if (d2 && !od2pressed)
                    dir2 = dynamicdir2 = Dir.Down;
                if (l2 && !ol2pressed)
                    dir2 = dynamicdir2 = Dir.Left;
                if (r2 && !or2pressed)
                    dir2 = dynamicdir2 = Dir.Right;
                if ((ou2pressed && !u2 && dynamicdir2 == Dir.Up) ||
                    (od2pressed && !d2 && dynamicdir2 == Dir.Down) ||
                    (ol2pressed && !l2 && dynamicdir2 == Dir.Left) ||
                    (or2pressed && !r2 && dynamicdir2 == Dir.Right))
                {
                    dynamicdir2 = Dir.None;
                    if (u2) dynamicdir2 = dir2 = 2;
                    if (d2) dynamicdir2 = dir2 = 6;
                    if (l2) dynamicdir2 = dir2 = 4;
                    if (r2) dynamicdir2 = dir2 = 0;
                }
                ou2pressed = u2;
                od2pressed = d2;
                ol2pressed = l2;
                or2pressed = r2;
                keydir2 = dir2;
                if (dynamicdir2 != Dir.None)
                    keydir2 = dynamicdir2;
                dir2 = Dir.None;
            }
        }

        /* Calibrate joystick while waiting at title screen. This works more
           effectively if the user waggles the joystick in the title screen. */
        public bool TestStart()
        {
            bool startf = false;
            if (shouldStart)
            {
                shouldStart = false;
                startf = true;
            }
            if (!startf)
                return false;

            return true;
        }

        /* Why the joystick reading is split between readdirect and getdir like this is a
           mystery to me. */
        public int GetDirect(int n)
        {
            int dir = ((n == 0) ? keydir : keydir2);
            if (n == 0)
            {
                if (game.record.IsPlaying)
                    game.record.PlayGetDirection(ref dir, ref firepflag);

                game.record.PutDirection(dir, firepflag);
            }
            else
            {
                if (game.record.IsPlaying)
                    game.record.PlayGetDirection(ref dir, ref fire2pflag);

                game.record.PutDirection(dir, fire2pflag);
            }

            return dir;
        }

        public bool IsKeyRemapped(int index)
        {
            return keyboard.IsKeyRemapped(index);
        }

        public int GetKey(bool isScanCode)
        {
            return keyboard.GetKey(isScanCode);
        }
    }
}