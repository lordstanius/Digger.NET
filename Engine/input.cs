/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public class Input
    {
        public SdlKeyboard keyboard = new SdlKeyboard();

        public const int NKEYS = 19;
        public const int DKEY_CHT = 10; /* Cheat */
        public const int DKEY_SUP = 11; /* Increase speed */
        public const int DKEY_SDN = 12; /* Decrease speed */
        public const int DKEY_MTG = 13; /* Toggle music */
        public const int DKEY_STG = 14; /* Toggle sound */
        public const int DKEY_EXT = 15; /* Exit */
        public const int DKEY_PUS = 16; /* Pause */
        public const int DKEY_MCH = 17; /* Mode change */
        public const int DKEY_SDR = 18; /* Save DRF */

        public bool[] krdf = new bool[NKEYS];

        public bool escape;
        public bool firepflag;
        public bool fire2pflag;
        public bool pausef;
        public bool mode_change;
        public bool aleftpressed = false;
        public bool arightpressed = false;
        public bool auppressed = false;
        public bool adownpressed = false;
        public bool start = false;
        public bool af1pressed = false;
        public bool aleft2pressed = false;
        public bool aright2pressed = false;
        public bool aup2pressed = false;
        public bool adown2pressed = false;
        public bool af12pressed = false;

        public int akeypressed;

        private int dynamicdir = -1, dynamicdir2 = -1, dir = -1, dir2 = -1, joyx = 0, joyy = 0;

        private bool joybut1 = false;
        private bool joyflag = false;

        private int keydir, keydir2, jleftthresh, jupthresh, jrightthresh, jdownthresh, joyanax, joyanay;

        public int prockey(int kn)
        {
            int key = keyboard.GetKey(true);
            if (kn != DKEY_EXT && key == keyboard.keycodes[DKEY_EXT][0])
                return -1;
            keyboard.keycodes[kn][0] = key;
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
        public void checkkeyb(Sound sound)
        {
            int k = 0;

            if (keyboard.IsLeftPressed)
                aleftpressed = true;
            if (keyboard.IsRightPressed)
                arightpressed = true;
            if (keyboard.IsUpPressed)
                auppressed = true;
            if (keyboard.IsDownPressed)
                adownpressed = true;
            if (keyboard.IsF1Pressed)
                af1pressed = true;
            if (keyboard.IsLeft2Pressed)
                aleft2pressed = true;
            if (keyboard.IsRight2Pressed)
                aright2pressed = true;
            if (keyboard.IsUp2Pressed)
                aup2pressed = true;
            if (keyboard.IsDown2Pressed)
                adown2pressed = true;
            if (keyboard.IsF12Pressed)
                af12pressed = true;

            while (keyboard.IsKeyboardHit())
            {
                akeypressed = keyboard.GetKey(true);
                for (int i = 0; i < 10; i++)
                    for (int j = 2; j < 5; j++)
                        if (akeypressed == keyboard.keycodes[i][j])
                            aflagp(i, true);

                for (int i = 10; i < NKEYS; i++)
                    for (int j = 0; j < 5; j++)
                        if (akeypressed == keyboard.keycodes[i][j])
                            k = i;
                switch (k)
                {
                    case DKEY_CHT: /* Cheat! */
                        if (!DiggerC.g_isGauntletMode)
                        {
                            DiggerC.record.playing = false;
                            DiggerC.record.drfvalid = false;
                        }
                        break;
                    case DKEY_SUP: /* Increase speed */
                        if (DiggerC.g_FrameTime > 10000)
                            DiggerC.g_FrameTime -= 10000;
                        break;
                    case DKEY_SDN: /* Decrease speed */
                        DiggerC.g_FrameTime += 10000;
                        break;
                    case DKEY_MTG: /* Toggle music */
                        sound.musicflag = !sound.musicflag;
                        break;
                    case DKEY_STG: /* Toggle sound */
                        sound.soundflag = !sound.soundflag;
                        break;
                    case DKEY_EXT: /* Exit */
                        escape = true;
                        break;
                    case DKEY_PUS: /* Pause */
                        pausef = true;
                        break;
                    case DKEY_MCH: /* Mode change */
                        mode_change = true;
                        break;
                    case DKEY_SDR: /* Save DRF */
                        DiggerC.record.savedrf = true;
                        break;
                }
                if (!mode_change)
                    start = true;                                /* Change number of players */
            }
        }

        private void aflagp(int i, bool value)
        {
            switch (i)
            {
                case 0: arightpressed = value; break;
                case 1: auppressed = value; break;
                case 2: aleftpressed = value; break;
                case 3: adownpressed = value; break;
                case 4: af1pressed = value; break;
                case 5: aright2pressed = value; break;
                case 6: aup2pressed = value; break;
                case 7: aleft2pressed = value; break;
                case 8: adown2pressed = value; break;
                case 9: af12pressed = value; break;
            }
        }

        /* Joystick not yet implemented. It will be, though, using gethrt on platform
           DOSPC. */
        public void readjoy()
        {
        }

        public void detectjoy()
        {
            joyflag = false;
            dir = dynamicdir = DiggerC.DIR_NONE;
        }

        /* Contrary to some beliefs, you don't need a separate OS call to flush the
           keyboard buffer. */
        public void flushkeybuf()
        {
            while (keyboard.IsKeyboardHit())
                keyboard.GetKey(true);

            aleftpressed = arightpressed = auppressed = adownpressed = af1pressed = false;
            aleft2pressed = aright2pressed = aup2pressed = adown2pressed = af12pressed = false;
        }

        public void clearfire(int n)
        {
            if (n == 0)
                af1pressed = false;
            else
                af12pressed = false;
        }

        public bool oupressed = false, odpressed = false, olpressed = false, orpressed = false;
        public bool ou2pressed = false, od2pressed = false, ol2pressed = false, or2pressed = false;

        public void readdirect(int n)
        {
            short j;
            bool u = false, d = false, l = false, r = false;
            bool u2 = false, d2 = false, l2 = false, r2 = false;

            if (n == 0)
            {
                if (auppressed || keyboard.IsUpPressed) { u = true; auppressed = false; }
                if (adownpressed || keyboard.IsDownPressed) { d = true; adownpressed = false; }
                if (aleftpressed || keyboard.IsLeftPressed) { l = true; aleftpressed = false; }
                if (arightpressed || keyboard.IsRightPressed) { r = true; arightpressed = false; }
                if (keyboard.IsF1Pressed || af1pressed)
                {
                    firepflag = true;
                    af1pressed = false;
                }
                else
                    firepflag = false;
                if (u && !oupressed)
                    dir = dynamicdir = DiggerC.DIR_UP;
                if (d && !odpressed)
                    dir = dynamicdir = DiggerC.DIR_DOWN;
                if (l && !olpressed)
                    dir = dynamicdir = DiggerC.DIR_LEFT;
                if (r && !orpressed)
                    dir = dynamicdir = DiggerC.DIR_RIGHT;
                if ((oupressed && !u && dynamicdir == DiggerC.DIR_UP) ||
                    (odpressed && !d && dynamicdir == DiggerC.DIR_DOWN) ||
                    (olpressed && !l && dynamicdir == DiggerC.DIR_LEFT) ||
                    (orpressed && !r && dynamicdir == DiggerC.DIR_RIGHT))
                {
                    dynamicdir = DiggerC.DIR_NONE;
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
                if (dynamicdir != DiggerC.DIR_NONE)
                    keydir = dynamicdir;
                dir = DiggerC.DIR_NONE;
            }
            else
            {
                if (aup2pressed || keyboard.IsUp2Pressed) { u2 = true; aup2pressed = false; }
                if (adown2pressed || keyboard.IsDown2Pressed) { d2 = true; adown2pressed = false; }
                if (aleft2pressed || keyboard.IsLeft2Pressed) { l2 = true; aleft2pressed = false; }
                if (aright2pressed || keyboard.IsRight2Pressed) { r2 = true; aright2pressed = false; }
                if (keyboard.IsF12Pressed || af12pressed)
                {
                    fire2pflag = true;
                    af12pressed = false;
                }
                else
                    fire2pflag = false;
                if (u2 && !ou2pressed)
                    dir2 = dynamicdir2 = DiggerC.DIR_UP;
                if (d2 && !od2pressed)
                    dir2 = dynamicdir2 = DiggerC.DIR_DOWN;
                if (l2 && !ol2pressed)
                    dir2 = dynamicdir2 = DiggerC.DIR_LEFT;
                if (r2 && !or2pressed)
                    dir2 = dynamicdir2 = DiggerC.DIR_RIGHT;
                if ((ou2pressed && !u2 && dynamicdir2 == DiggerC.DIR_UP) ||
                    (od2pressed && !d2 && dynamicdir2 == DiggerC.DIR_DOWN) ||
                    (ol2pressed && !l2 && dynamicdir2 == DiggerC.DIR_LEFT) ||
                    (or2pressed && !r2 && dynamicdir2 == DiggerC.DIR_RIGHT))
                {
                    dynamicdir2 = DiggerC.DIR_NONE;
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
                if (dynamicdir2 != DiggerC.DIR_NONE)
                    keydir2 = dynamicdir2;
                dir2 = DiggerC.DIR_NONE;
            }

            if (joyflag)
            {
                DiggerC.incpenalty();
                DiggerC.incpenalty();
                joyanay = 0;
                joyanax = 0;
                for (j = 0; j < 4; j++)
                {
                    readjoy();
                    joyanax += joyx;
                    joyanay += joyy;
                }
                joyx = joyanax >> 2;
                joyy = joyanay >> 2;
                if (joybut1)
                    firepflag = true;
                else
                    firepflag = false;
            }
        }

        /* Calibrate joystick while waiting at title screen. This works more
           effectively if the user waggles the joystick in the title screen. */
        public bool teststart()
        {
            short j;
            bool startf = false;
            if (joyflag)
            {
                readjoy();
                if (joybut1)
                    startf = true;
            }
            if (start)
            {
                start = false;
                startf = true;
                joyflag = false;
            }
            if (!startf)
                return false;
            if (joyflag)
            {
                joyanay = 0;
                joyanax = 0;
                for (j = 0; j < 50; j++)
                {
                    readjoy();
                    joyanax += joyx;
                    joyanay += joyy;
                }
                joyx = joyanax / 50;
                joyy = joyanay / 50;
                jleftthresh = joyx - 35;
                if (jleftthresh < 0)
                    jleftthresh = 0;
                jleftthresh += 10;
                jupthresh = joyy - 35;
                if (jupthresh < 0)
                    jupthresh = 0;
                jupthresh += 10;
                jrightthresh = joyx + 35;
                if (jrightthresh > 255)
                    jrightthresh = 255;
                jrightthresh -= 10;
                jdownthresh = joyy + 35;
                if (jdownthresh > 255)
                    jdownthresh = 255;
                jdownthresh -= 10;
                joyanax = joyx;
                joyanay = joyy;
            }
            return true;
        }

        /* Why the joystick reading is split between readdirect and getdir like this is a
           mystery to me. */
        public int getdirect(int n)
        {
            int dir = ((n == 0) ? keydir : keydir2);
            if (joyflag)
            {
                dir = DiggerC.DIR_NONE;
                if (joyx < jleftthresh)
                    dir = DiggerC.DIR_LEFT;
                if (joyx > jrightthresh)
                    dir = DiggerC.DIR_RIGHT;
                if (joyx >= jleftthresh && joyx <= jrightthresh)
                {
                    if (joyy < jupthresh)
                        dir = DiggerC.DIR_UP;
                    if (joyy > jdownthresh)
                        dir = DiggerC.DIR_DOWN;
                }
            }
            if (n == 0)
            {
                if (DiggerC.record.playing)
                    DiggerC.record.playgetdir(ref dir, ref firepflag);
                DiggerC.record.recputdir(dir, firepflag);
            }
            else
            {
                if (DiggerC.record.playing)
                    DiggerC.record.playgetdir(ref dir, ref fire2pflag);
                DiggerC.record.recputdir(dir, fire2pflag);
            }

            return dir;
        }
    }
}