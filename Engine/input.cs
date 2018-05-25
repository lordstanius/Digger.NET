/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public static partial class DiggerC
    {
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


        public static bool[] krdf = new bool[NKEYS];

        public static bool escape;
        public static bool firepflag;
        public static bool fire2pflag;
        public static bool pausef;
        public static bool mode_change;
        public static bool aleftpressed = false;
        public static bool arightpressed = false;
        public static bool auppressed = false;
        public static bool adownpressed = false;
        public static bool start = false;
        public static bool af1pressed = false;
        public static bool aleft2pressed = false;
        public static bool aright2pressed = false;
        public static bool aup2pressed = false;
        public static bool adown2pressed = false;
        public static bool af12pressed = false;

        public static int akeypressed;

        private static int dynamicdir = -1, dynamicdir2 = -1, staticdir = -1, staticdir2 = -1, joyx = 0, joyy = 0;

        private static bool joybut1 = false;
        private static bool joyflag = false;

        private static int keydir, keydir2, jleftthresh, jupthresh, jrightthresh, jdownthresh, joyanax, joyanay;


        /* The standard ASCII keyboard is also checked so that very short keypresses
           are not overlooked. The functions kbhit() (returns bool denoting whether or
           not there is a key in the buffer) and getkey() (wait until a key is in the
           buffer, then return it) are used. These functions are emulated on platforms
           which only provide an inkey() function (return the key in the buffer, unless
           there is none, in which case return -1. It is done this way around for
           historical reasons, there is no fundamental reason why it shouldn't be the
           other way around. */
        public static void checkkeyb()
        {
            int k = 0;

            if (leftpressed)
                aleftpressed = true;
            if (rightpressed)
                arightpressed = true;
            if (uppressed)
                auppressed = true;
            if (downpressed)
                adownpressed = true;
            if (f1pressed)
                af1pressed = true;
            if (left2pressed)
                aleft2pressed = true;
            if (right2pressed)
                aright2pressed = true;
            if (up2pressed)
                aup2pressed = true;
            if (down2pressed)
                adown2pressed = true;
            if (f12pressed)
                af12pressed = true;

            while (kbhit())
            {
                akeypressed = getkey(true);
                for (int i = 0; i < 10; i++)
                    for (int j = 2; j < 5; j++)
                        if (akeypressed == keycodes[i][j])
                            aflagp(i, true);

                for (int i = 10; i < NKEYS; i++)
                    for (int j = 0; j < 5; j++)
                        if (akeypressed == keycodes[i][j])
                            k = i;
                switch (k)
                {
                    case DKEY_CHT: /* Cheat! */
                        if (!g_isGauntletMode)
                        {
                            playing = false;
                            drfvalid = false;
                        }
                        break;
                    case DKEY_SUP: /* Increase speed */
                        if (g_FrameTime > 10000)
                            g_FrameTime -= 10000;
                        break;
                    case DKEY_SDN: /* Decrease speed */
                        g_FrameTime += 10000;
                        break;
                    case DKEY_MTG: /* Toggle music */
                        musicflag = !musicflag;
                        break;
                    case DKEY_STG: /* Toggle sound */
                        soundflag = !soundflag;
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
                        savedrf = true;
                        break;
                }
                if (!mode_change)
                    start = true;                                /* Change number of players */
            }
        }

        private static void aflagp(int i, bool value)
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
        public static void readjoy()
        {
        }

        public static void detectjoy()
        {
            joyflag = false;
            staticdir = dynamicdir = DIR_NONE;
        }

        /* Contrary to some beliefs, you don't need a separate OS call to flush the
           keyboard buffer. */
        public static void flushkeybuf()
        {
            while (kbhit())
                getkey(true);
            aleftpressed = arightpressed = auppressed = adownpressed = af1pressed = false;
            aleft2pressed = aright2pressed = aup2pressed = adown2pressed = af12pressed = false;
        }

        public static void clearfire(int n)
        {
            if (n == 0)
                af1pressed = false;
            else
                af12pressed = false;
        }

        public static bool oupressed = false, odpressed = false, olpressed = false, orpressed = false;
        public static bool ou2pressed = false, od2pressed = false, ol2pressed = false, or2pressed = false;

        public static void readdirect(int n)
        {
            short j;
            bool u = false, d = false, l = false, r = false;
            bool u2 = false, d2 = false, l2 = false, r2 = false;

            if (n == 0)
            {
                if (auppressed || uppressed) { u = true; auppressed = false; }
                if (adownpressed || downpressed) { d = true; adownpressed = false; }
                if (aleftpressed || leftpressed) { l = true; aleftpressed = false; }
                if (arightpressed || rightpressed) { r = true; arightpressed = false; }
                if (f1pressed || af1pressed)
                {
                    firepflag = true;
                    af1pressed = false;
                }
                else
                    firepflag = false;
                if (u && !oupressed)
                    staticdir = dynamicdir = DIR_UP;
                if (d && !odpressed)
                    staticdir = dynamicdir = DIR_DOWN;
                if (l && !olpressed)
                    staticdir = dynamicdir = DIR_LEFT;
                if (r && !orpressed)
                    staticdir = dynamicdir = DIR_RIGHT;
                if ((oupressed && !u && dynamicdir == DIR_UP) ||
                    (odpressed && !d && dynamicdir == DIR_DOWN) ||
                    (olpressed && !l && dynamicdir == DIR_LEFT) ||
                    (orpressed && !r && dynamicdir == DIR_RIGHT))
                {
                    dynamicdir = DIR_NONE;
                    if (u) dynamicdir = staticdir = 2;
                    if (d) dynamicdir = staticdir = 6;
                    if (l) dynamicdir = staticdir = 4;
                    if (r) dynamicdir = staticdir = 0;
                }
                oupressed = u;
                odpressed = d;
                olpressed = l;
                orpressed = r;
                keydir = staticdir;
                if (dynamicdir != DIR_NONE)
                    keydir = dynamicdir;
                staticdir = DIR_NONE;
            }
            else
            {
                if (aup2pressed || up2pressed) { u2 = true; aup2pressed = false; }
                if (adown2pressed || down2pressed) { d2 = true; adown2pressed = false; }
                if (aleft2pressed || left2pressed) { l2 = true; aleft2pressed = false; }
                if (aright2pressed || right2pressed) { r2 = true; aright2pressed = false; }
                if (f12pressed || af12pressed)
                {
                    fire2pflag = true;
                    af12pressed = false;
                }
                else
                    fire2pflag = false;
                if (u2 && !ou2pressed)
                    staticdir2 = dynamicdir2 = DIR_UP;
                if (d2 && !od2pressed)
                    staticdir2 = dynamicdir2 = DIR_DOWN;
                if (l2 && !ol2pressed)
                    staticdir2 = dynamicdir2 = DIR_LEFT;
                if (r2 && !or2pressed)
                    staticdir2 = dynamicdir2 = DIR_RIGHT;
                if ((ou2pressed && !u2 && dynamicdir2 == DIR_UP) ||
                    (od2pressed && !d2 && dynamicdir2 == DIR_DOWN) ||
                    (ol2pressed && !l2 && dynamicdir2 == DIR_LEFT) ||
                    (or2pressed && !r2 && dynamicdir2 == DIR_RIGHT))
                {
                    dynamicdir2 = DIR_NONE;
                    if (u2) dynamicdir2 = staticdir2 = 2;
                    if (d2) dynamicdir2 = staticdir2 = 6;
                    if (l2) dynamicdir2 = staticdir2 = 4;
                    if (r2) dynamicdir2 = staticdir2 = 0;
                }
                ou2pressed = u2;
                od2pressed = d2;
                ol2pressed = l2;
                or2pressed = r2;
                keydir2 = staticdir2;
                if (dynamicdir2 != DIR_NONE)
                    keydir2 = dynamicdir2;
                staticdir2 = DIR_NONE;
            }

            if (joyflag)
            {
                incpenalty();
                incpenalty();
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
        public static bool teststart()
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
        public static int getdirect(int n)
        {
            int dir = ((n == 0) ? keydir : keydir2);
            if (joyflag)
            {
                dir = DIR_NONE;
                if (joyx < jleftthresh)
                    dir = DIR_LEFT;
                if (joyx > jrightthresh)
                    dir = DIR_RIGHT;
                if (joyx >= jleftthresh && joyx <= jrightthresh)
                {
                    if (joyy < jupthresh)
                        dir = DIR_UP;
                    if (joyy > jdownthresh)
                        dir = DIR_DOWN;
                }
            }
            if (n == 0)
            {
                if (playing)
                    playgetdir(ref dir, ref firepflag);
                recputdir(dir, firepflag);
            }
            else
            {
                if (playing)
                    playgetdir(ref dir, ref fire2pflag);
                recputdir(dir, fire2pflag);
            }

            return dir;
        }
    }
}