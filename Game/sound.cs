/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using SDL2;
using System;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public static int wavetype = 0, musvol = 0;
        public static int spkrmode = 0, timerrate = 0x7d0;
        public static ushort timercount = 0, t2val = 0, t0val = 0;
        public static int pulsewidth = 1;
        public static int volume = 0;

        public static byte timerclock = 0;

        public static bool soundflag = true, musicflag = true;

        public static Action setupsound = s0setupsound;
        public static Action killsound = s0killsound;
        public static Action fillbuffer = s0fillbuffer;
        public static Action initint8 = s0initint8;
        public static Action restoreint8 = s0restoreint8;
        public static Action soundoff = s0soundoff;
        public static Action setspkrt2 = s0setspkrt2;
        public static Action<ushort> settimer0 = s0settimer0;
        public static Action<ushort> timer0 = s0timer0;
        public static Action<ushort> settimer2 = s0settimer2;
        public static Action<ushort> timer2 = s0timer2;
        public static Action soundkillglob = s0soundkillglob;

        public static bool sndflag = false, soundpausedflag = false;

        public static uint randvs = 0;

        public static uint randnos(int n)
        {
            randvs = randvs * 0x15a4e35 + 1;
            return (uint)((randvs & 0x7fffffff) % n);
        }

        public static void sett2val(ushort t2v)
        {
            if (sndflag)
                timer2(t2v);
        }

        public static void soundint()
        {
            timerclock++;
            if (soundflag && !sndflag)
                sndflag = musicflag = true;
            if (!soundflag && sndflag)
            {
                sndflag = false;
                timer2(40);
                setsoundt2();
                soundoff();
            }
            if (sndflag && !soundpausedflag)
            {
                t0val = 0x7d00;
                t2val = 40;
                if (musicflag)
                    musicupdate();
                soundemeraldupdate();
                soundwobbleupdate();
                soundddieupdate();
                soundbreakupdate();
                soundgoldupdate();
                soundemupdate();
                soundexplodeupdate();
                soundfireupdate();
                soundeatmupdate();
                soundfallupdate();
                sound1upupdate();
                soundbonusupdate();
                if (t0val == 0x7d00 || t2val != 40)
                    setsoundt2();
                else
                {
                    setsoundmode();
                    sett0();
                }
                sett2val(t2val);
            }
        }

        public static void soundstop()
        {
            int i;
            soundfalloff();
            soundwobbleoff();
            for (i = 0; i < FIREBALLS; i++)
                soundfireoff(i);
            musicoff();
            soundbonusoff();
            for (i = 0; i < FIREBALLS; i++)
                soundexplodeoff(i);
            soundbreakoff();
            soundemoff();
            soundemeraldoff();
            soundgoldoff();
            soundeatmoff();
            soundddieoff();
            sound1upoff();
        }


        public static bool soundlevdoneflag = false;
        public static short nljpointer = 0, nljnoteduration = 0;

        public static void soundlevdone()
        {
            short timer = 0;
            soundstop();
            nljpointer = 0;
            nljnoteduration = 20;
            soundlevdoneflag = soundpausedflag = true;
            ftime /= 5;
            while (soundlevdoneflag && !escape)
            {
                fillbuffer();
                if (!wave_device_available)
                    soundlevdoneflag = false;

                gethrt();	/* Let some CPU time go away */
                soundint();

                if (timerclock == timer)
                    continue;
                soundlevdoneupdate();
                checkkeyb();
                timer = timerclock;
            }
            ftime *= 5;
            soundlevdoneoff();
        }

        public static void soundlevdoneoff()
        {
            soundlevdoneflag = soundpausedflag = false;
        }

        public static ushort[] newlevjingle = { 0x8e8, 0x712, 0x5f2, 0x7f0, 0x6ac, 0x54c, 0x712, 0x5f2, 0x4b8, 0x474, 0x474 };

        public static void soundlevdoneupdate()
        {
            if (sndflag)
            {
                if (nljpointer < 11)
                    t2val = newlevjingle[nljpointer];
                t0val = (ushort)(t2val + 35);
                musvol = 50;
                setsoundmode();
                sett0();
                sett2val(t2val);
                if (nljnoteduration > 0)
                    nljnoteduration--;
                else
                {
                    nljnoteduration = 20;
                    nljpointer++;
                    if (nljpointer > 10)
                        soundlevdoneoff();
                }
            }
            else
                soundlevdoneflag = false;
        }


        public static bool soundfallflag = false, soundfallf = false;
        public static ushort soundfallvalue, soundfalln = 0;

        public static void soundfall()
        {
            soundfallvalue = 1000;
            soundfallflag = true;
        }

        public static void soundfalloff()
        {
            soundfallflag = false;
            soundfalln = 0;
        }

        public static void soundfallupdate()
        {
            if (soundfallflag)
            {
                if (soundfalln < 1)
                {
                    soundfalln++;
                    if (soundfallf)
                        t2val = soundfallvalue;
                }
                else
                {
                    soundfalln = 0;
                    if (soundfallf)
                    {
                        soundfallvalue += 50;
                        soundfallf = false;
                    }
                    else
                        soundfallf = true;
                }
            }
        }


        public static bool soundbreakflag = false;
        public static ushort soundbreakduration = 0, soundbreakvalue = 0;

        public static void soundbreak()
        {
            soundbreakduration = 3;
            if (soundbreakvalue < 15000)
                soundbreakvalue = 15000;
            soundbreakflag = true;
        }

        public static void soundbreakoff()
        {
            soundbreakflag = false;
        }

        public static void soundbreakupdate()
        {
            if (soundbreakflag)
            {
                if (soundbreakduration != 0)
                {
                    soundbreakduration--;
                    t2val = soundbreakvalue;
                }
                else
                    soundbreakflag = false;
            }
        }


        public static bool soundwobbleflag = false;
        public static short soundwobblen = 0;

        public static void soundwobble()
        {
            soundwobbleflag = true;
        }

        public static void soundwobbleoff()
        {
            soundwobbleflag = false;
            soundwobblen = 0;
        }

        public static void soundwobbleupdate()
        {
            if (soundwobbleflag)
            {
                soundwobblen++;
                if (soundwobblen > 63)
                    soundwobblen = 0;
                switch (soundwobblen)
                {
                    case 0:
                        t2val = 0x7d0;
                        break;
                    case 16:
                    case 48:
                        t2val = 0x9c4;
                        break;
                    case 32:
                        t2val = 0xbb8;
                        break;
                }
            }
        }

        public static bool[] soundfireflag = new bool[FIREBALLS];
        public static bool[] sff = new bool[FIREBALLS];
        public static ushort[] soundfirevalue = new ushort[FIREBALLS];
        public static ushort[] soundfiren = new ushort[FIREBALLS];
        public static int soundfirew = 0;

        public static void soundfire(int n)
        {
            soundfirevalue[n] = 500;
            soundfireflag[n] = true;
        }

        public static void soundfireoff(int n)
        {
            soundfireflag[n] = false;
            soundfiren[n] = 0;
        }

        public static void soundfireupdate()
        {
            int n;
            bool f = false;
            for (n = 0; n < FIREBALLS; n++)
            {
                sff[n] = false;
                if (soundfireflag[n])
                {
                    if (soundfiren[n] == 1)
                    {
                        soundfiren[n] = 0;
                        soundfirevalue[n] += (ushort)(soundfirevalue[n] / 55);
                        sff[n] = true;
                        f = true;
                        if (soundfirevalue[n] > 30000)
                            soundfireoff(n);
                    }
                    else
                        soundfiren[n]++;
                }
            }
            if (f)
            {
                do
                {
                    n = soundfirew++;
                    if (soundfirew == FIREBALLS)
                        soundfirew = 0;
                } while (!sff[n]);
                t2val = (ushort)(soundfirevalue[n] + randnos(soundfirevalue[n] >> 3));
            }
        }

        public static bool[] soundexplodeflag = new bool[FIREBALLS];
        public static bool[] sef = new bool[FIREBALLS];
        public static ushort[] soundexplodevalue = new ushort[FIREBALLS];
        public static ushort[] soundexplodeduration = new ushort[FIREBALLS];
        public static int soundexplodew = 0;

        public static void soundexplode(int n)
        {
            soundexplodevalue[n] = 1500;
            soundexplodeduration[n] = 10;
            soundexplodeflag[n] = true;
            soundfireoff(n);
        }

        public static void soundexplodeoff(int n)
        {
            soundexplodeflag[n] = false;
        }

        public static void soundexplodeupdate()
        {
            int n;
            bool f = false;
            for (n = 0; n < FIREBALLS; n++)
            {
                sef[n] = false;
                if (soundexplodeflag[n])
                {
                    if (soundexplodeduration[n] != 0)
                    {
                        soundexplodevalue[n] = (ushort)(soundexplodevalue[n] - (soundexplodevalue[n] >> 3));
                        soundexplodeduration[n]--;
                        sef[n] = true;
                        f = true;
                    }
                    else
                        soundexplodeflag[n] = false;
                }
            }
            if (f)
            {
                do
                {
                    n = soundexplodew++;
                    if (soundexplodew == FIREBALLS)
                        soundexplodew = 0;
                } while (!sef[n]);
                t2val = soundexplodevalue[n];
            }
        }


        public static bool soundbonusflag = false;
        public static short soundbonusn = 0;

        public static void soundbonus()
        {
            soundbonusflag = true;
        }

        public static void soundbonusoff()
        {
            soundbonusflag = false;
            soundbonusn = 0;
        }

        public static void soundbonusupdate()
        {
            if (soundbonusflag)
            {
                soundbonusn++;
                if (soundbonusn > 15)
                    soundbonusn = 0;
                if (soundbonusn >= 0 && soundbonusn < 6)
                    t2val = 0x4ce;
                if (soundbonusn >= 8 && soundbonusn < 14)
                    t2val = 0x5e9;
            }
        }


        public static bool soundemflag = false;

        public static void soundem()
        {
            soundemflag = true;
        }

        public static void soundemoff()
        {
            soundemflag = false;
        }

        public static void soundemupdate()
        {
            if (soundemflag)
            {
                t2val = 1000;
                soundemoff();
            }
        }


        public static bool soundemeraldflag = false;
        public static ushort soundemeraldduration, emerfreq, soundemeraldn;

        public static ushort[] emfreqs = { 0x8e8, 0x7f0, 0x712, 0x6ac, 0x5f2, 0x54c, 0x4b8, 0x474 };

        public static void soundemerald(int n)
        {
            emerfreq = emfreqs[n];
            soundemeraldduration = 7;
            soundemeraldn = 0;
            soundemeraldflag = true;
        }

        public static void soundemeraldoff()
        {
            soundemeraldflag = false;
        }

        public static void soundemeraldupdate()
        {
            if (soundemeraldflag)
            {
                if (soundemeraldduration != 0)
                {
                    if (soundemeraldn == 0 || soundemeraldn == 1)
                        t2val = emerfreq;
                    soundemeraldn++;
                    if (soundemeraldn > 7)
                    {
                        soundemeraldn = 0;
                        soundemeraldduration--;
                    }
                }
                else
                    soundemeraldoff();
            }
        }


        public static bool soundgoldflag = false, soundgoldf = false;
        public static ushort soundgoldvalue1, soundgoldvalue2, soundgoldduration;

        public static void soundgold()
        {
            soundgoldvalue1 = 500;
            soundgoldvalue2 = 4000;
            soundgoldduration = 30;
            soundgoldf = false;
            soundgoldflag = true;
        }

        public static void soundgoldoff()
        {
            soundgoldflag = false;
        }

        public static void soundgoldupdate()
        {
            if (soundgoldflag)
            {
                if (soundgoldduration != 0)
                    soundgoldduration--;
                else
                    soundgoldflag = false;
                if (soundgoldf)
                {
                    soundgoldf = false;
                    t2val = soundgoldvalue1;
                }
                else
                {
                    soundgoldf = true;
                    t2val = soundgoldvalue2;
                }
                soundgoldvalue1 += (ushort)(soundgoldvalue1 >> 4);
                soundgoldvalue2 -= (ushort)(soundgoldvalue2 >> 4);
            }
        }

        public static bool soundeatmflag = false;
        public static ushort soundeatmvalue, soundeatmduration, soundeatmn;

        public static void soundeatm()
        {
            soundeatmduration = 20;
            soundeatmn = 3;
            soundeatmvalue = 2000;
            soundeatmflag = true;
        }

        public static void soundeatmoff()
        {
            soundeatmflag = false;
        }

        public static void soundeatmupdate()
        {
            if (soundeatmflag)
            {
                if (soundeatmn != 0)
                {
                    if (soundeatmduration != 0)
                    {
                        if ((soundeatmduration % 4) == 1)
                            t2val = soundeatmvalue;
                        if ((soundeatmduration % 4) == 3)
                            t2val = (ushort)(soundeatmvalue - (soundeatmvalue >> 4));
                        soundeatmduration--;
                        soundeatmvalue -= (ushort)(soundeatmvalue >> 4);
                    }
                    else
                    {
                        soundeatmduration = 20;
                        soundeatmn--;
                        soundeatmvalue = 2000;
                    }
                }
                else
                    soundeatmflag = false;
            }
        }


        public static bool soundddieflag = false;
        public static ushort soundddien, soundddievalue;

        public static void soundddie()
        {
            soundddien = 0;
            soundddievalue = 20000;
            soundddieflag = true;
        }

        public static void soundddieoff()
        {
            soundddieflag = false;
        }

        public static void soundddieupdate()
        {
            if (soundddieflag)
            {
                soundddien++;
                if (soundddien == 1)
                    musicoff();
                if (soundddien >= 1 && soundddien <= 10)
                    soundddievalue = (ushort)(20000 - soundddien * 1000);
                if (soundddien > 10)
                    soundddievalue += 500;
                if (soundddievalue > 30000)
                    soundddieoff();
                t2val = soundddievalue;
            }
        }


        public static bool sound1upflag = false;
        public static short sound1upduration = 0;

        public static void sound1up()
        {
            sound1upduration = 96;
            sound1upflag = true;
        }

        public static void sound1upoff()
        {
            sound1upflag = false;
        }

        public static void sound1upupdate()
        {
            if (sound1upflag)
            {
                if ((sound1upduration / 3) % 2 != 0)
                    t2val = (ushort)((sound1upduration << 2) + 600);
                sound1upduration--;
                if (sound1upduration < 1)
                    sound1upflag = false;
            }
        }


        public static bool musicplaying = false;
        public static short musicp = 0, tuneno = 0, noteduration = 0, notevalue = 0, musicmaxvol = 0,
              musicattackrate = 0, musicsustainlevel = 0, musicdecayrate = 0, musicnotewidth = 0,
              musicreleaserate = 0, musicstage = 0, musicn = 0;

        public static void music(short tune)
        {
            tuneno = tune;
            musicp = 0;
            noteduration = 0;
            switch (tune)
            {
                case 0:
                    musicmaxvol = 50;
                    musicattackrate = 20;
                    musicsustainlevel = 20;
                    musicdecayrate = 10;
                    musicreleaserate = 4;
                    break;
                case 1:
                    musicmaxvol = 50;
                    musicattackrate = 50;
                    musicsustainlevel = 8;
                    musicdecayrate = 15;
                    musicreleaserate = 1;
                    break;
                case 2:
                    musicmaxvol = 50;
                    musicattackrate = 50;
                    musicsustainlevel = 25;
                    musicdecayrate = 5;
                    musicreleaserate = 1;
                    break;
            }
            musicplaying = true;
            if (tune == 2)
                soundddieoff();
        }

        public static void musicoff()
        {
            musicplaying = false;
            musicp = 0;
        }

        public static short[] bonusjingle = {
            0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2,
            0xd59,4, 0xbe4,4, 0xa98,4,0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2,
            0x11d1,4, 0xd59,2, 0xa98,2, 0xbe4,4, 0xe24,4,0x11d1,4,0x11d1,2,0x11d1,2,
            0x11d1,4,0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2, 0xd59,4, 0xbe4,4,
            0xa98,4, 0xd59,2, 0xa98,2, 0x8e8,10,0xa00,2, 0xa98,2, 0xbe4,2, 0xd59,4,
            0xa98,4, 0xd59,4,0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2,0x11d1,4,
            0x11d1,2,0x11d1,2, 0xd59,4, 0xbe4,4, 0xa98,4,0x11d1,2,0x11d1,2,0x11d1,4,
            0x11d1,2,0x11d1,2,0x11d1,4, 0xd59,2, 0xa98,2, 0xbe4,4, 0xe24,4,0x11d1,4,
            0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2,0x11d1,4,0x11d1,2,0x11d1,2,
            0xd59,4, 0xbe4,4, 0xa98,4, 0xd59,2, 0xa98,2, 0x8e8,10,0xa00,2, 0xa98,2,
            0xbe4,2, 0xd59,4, 0xa98,4, 0xd59,4, 0xa98,2, 0xa98,2, 0xa98,4, 0xa98,2,
            0xa98,2, 0xa98,4, 0xa98,2, 0xa98,2, 0xa98,4, 0x7f0,4, 0xa98,4, 0x7f0,4,
            0xa98,4, 0x7f0,4, 0xa98,4, 0xbe4,4, 0xd59,4, 0xe24,4, 0xfdf,4, 0xa98,2,
            0xa98,2, 0xa98,4, 0xa98,2, 0xa98,2, 0xa98,4, 0xa98,2, 0xa98,2, 0xa98,4,
            0x7f0,4, 0xa98,4, 0x7f0,4, 0xa98,4, 0x7f0,4, 0x8e8,4, 0x970,4, 0x8e8,4,
            0x970,4, 0x8e8,4, 0xa98,2, 0xa98,2, 0xa98,4, 0xa98,2, 0xa98,2, 0xa98,4,
            0xa98,2, 0xa98,2, 0xa98,4, 0x7f0,4, 0xa98,4, 0x7f0,4, 0xa98,4, 0x7f0,4,
            0xa98,4, 0xbe4,4, 0xd59,4, 0xe24,4, 0xfdf,4, 0xa98,2, 0xa98,2, 0xa98,4,
            0xa98,2, 0xa98,2, 0xa98,4, 0xa98,2, 0xa98,2, 0xa98,4, 0x7f0,4, 0xa98,4,
            0x7f0,4, 0xa98,4, 0x7f0,4, 0x8e8,4, 0x970,4, 0x8e8,4, 0x970,4, 0x8e8,4,
            0x7d64};

        public static short[] backgjingle ={
            0xfdf,2,0x11d1,2, 0xfdf,2,0x1530,2,0x1ab2,2,0x1530,2,0x1fbf,4, 0xfdf,2,
            0x11d1,2, 0xfdf,2,0x1530,2,0x1ab2,2,0x1530,2,0x1fbf,4, 0xfdf,2, 0xe24,2,
            0xd59,2, 0xe24,2, 0xd59,2, 0xfdf,2, 0xe24,2, 0xfdf,2, 0xe24,2,0x11d1,2,
            0xfdf,2,0x11d1,2, 0xfdf,2,0x1400,2, 0xfdf,4, 0xfdf,2,0x11d1,2, 0xfdf,2,
            0x1530,2,0x1ab2,2,0x1530,2,0x1fbf,4, 0xfdf,2,0x11d1,2, 0xfdf,2,0x1530,2,
            0x1ab2,2,0x1530,2,0x1fbf,4, 0xfdf,2, 0xe24,2, 0xd59,2, 0xe24,2, 0xd59,2,
            0xfdf,2, 0xe24,2, 0xfdf,2, 0xe24,2,0x11d1,2, 0xfdf,2,0x11d1,2, 0xfdf,2,
            0xe24,2, 0xd59,4, 0xa98,2, 0xbe4,2, 0xa98,2, 0xd59,2,0x11d1,2, 0xd59,2,
            0x1530,4, 0xa98,2, 0xbe4,2, 0xa98,2, 0xd59,2,0x11d1,2, 0xd59,2,0x1530,4,
            0xa98,2, 0x970,2, 0x8e8,2, 0x970,2, 0x8e8,2, 0xa98,2, 0x970,2, 0xa98,2,
            0x970,2, 0xbe4,2, 0xa98,2, 0xbe4,2, 0xa98,2, 0xd59,2, 0xa98,4, 0xa98,2,
            0xbe4,2, 0xa98,2, 0xd59,2,0x11d1,2, 0xd59,2,0x1530,4, 0xa98,2, 0xbe4,2,
            0xa98,2, 0xd59,2,0x11d1,2, 0xd59,2,0x1530,4, 0xa98,2, 0x970,2, 0x8e8,2,
            0x970,2, 0x8e8,2, 0xa98,2, 0x970,2, 0xa98,2, 0x970,2, 0xbe4,2, 0xa98,2,
            0xbe4,2, 0xa98,2, 0xd59,2, 0xa98,4, 0x7f0,2, 0x8e8,2, 0xa98,2, 0xd59,2,
            0x11d1,2, 0xd59,2,0x1530,4, 0xa98,2, 0xbe4,2, 0xa98,2, 0xd59,2,0x11d1,2,
            0xd59,2,0x1530,4, 0xa98,2, 0x970,2, 0x8e8,2, 0x970,2, 0x8e8,2, 0xa98,2,
            0x970,2, 0xa98,2, 0x970,2, 0xbe4,2, 0xa98,2, 0xbe4,2, 0xd59,2, 0xbe4,2,
            0xa98,4,0x7d64};

        public static short[] dirge ={
            0x7d00, 2,0x11d1, 6,0x11d1, 4,0x11d1, 2,0x11d1, 6, 0xefb, 4, 0xfdf, 2,
            0xfdf, 4,0x11d1, 2,0x11d1, 4,0x12e0, 2,0x11d1,12,0x7d00,16,0x7d00,16,
            0x7d00,16,0x7d00,16,0x7d00,16,0x7d00,16,0x7d00,16,0x7d00,16,0x7d00,16,
            0x7d00,16,0x7d00,16,0x7d00,16,0x7d64};

        public static void musicupdate()
        {
            if (!musicplaying)
                return;
            if (noteduration != 0)
                noteduration--;
            else
            {
                musicstage = musicn = 0;
                switch (tuneno)
                {
                    case 0:
                        noteduration = (short)(bonusjingle[musicp + 1] * 3);
                        musicnotewidth = (short)(noteduration - 3);
                        notevalue = bonusjingle[musicp];
                        musicp += 2;
                        if (bonusjingle[musicp] == 0x7d64)
                            musicp = 0;
                        break;
                    case 1:
                        noteduration = (short)(backgjingle[musicp + 1] * 6);
                        musicnotewidth = 12;
                        notevalue = backgjingle[musicp];
                        musicp += 2;
                        if (backgjingle[musicp] == 0x7d64)
                            musicp = 0;
                        break;
                    case 2:
                        noteduration = (short)(dirge[musicp + 1] * 10);
                        musicnotewidth = (short)(noteduration - 10);
                        notevalue = dirge[musicp];
                        musicp += 2;
                        if (dirge[musicp] == 0x7d64)
                            musicp = 0;
                        break;
                }
            }
            musicn++;
            wavetype = 1;
            t0val = (ushort)notevalue;
            if (musicn >= musicnotewidth)
                musicstage = 2;
            switch (musicstage)
            {
                case 0:
                    if (musvol + musicattackrate >= musicmaxvol)
                    {
                        musicstage = 1;
                        musvol = musicmaxvol;
                        break;
                    }
                    musvol += musicattackrate;
                    break;
                case 1:
                    if (musvol - musicdecayrate <= musicsustainlevel)
                    {
                        musvol = musicsustainlevel;
                        break;
                    }
                    musvol -= musicdecayrate;
                    break;
                case 2:
                    if (musvol - musicreleaserate <= 1)
                    {
                        musvol = 1;
                        break;
                    }
                    musvol -= musicreleaserate;
                    break;
            }
            if (musvol == 1)
                t0val = 0x7d00;
        }


        public static void soundpause()
        {
            soundpausedflag = true;
            SDL.PauseAudio(1);
        }

        public static void soundpauseoff()
        {
            soundpausedflag = false;
            SDL.PauseAudio(0);
        }

        public static void sett0()
        {
            if (sndflag)
            {
                timer2(t2val);
                if (t0val < 1000 && (wavetype == 1 || wavetype == 2))
                    t0val = 1000;
                timer0(t0val);
                timerrate = t0val;
                if (musvol < 1)
                    musvol = 1;
                if (musvol > 50)
                    musvol = 50;
                pulsewidth = musvol * volume;
                setsoundmode();
            }
        }

        public static bool soundt0flag = false;

        public static void setsoundt2()
        {
            if (soundt0flag)
            {
                spkrmode = 0;
                soundt0flag = false;
                setspkrt2();
            }
        }

        public static void setsoundmode()
        {
            spkrmode = wavetype;
            if (!soundt0flag && sndflag)
            {
                soundt0flag = true;
                setspkrt2();
            }
        }

        public static bool int8flag = false;

        public static void startint8()
        {
            if (!int8flag)
            {
                initint8();
                timerrate = 0x4000;
                settimer0(0x4000);
                int8flag = true;
            }
        }

        public static void stopint8()
        {
            settimer0(0);
            if (int8flag)
            {
                restoreint8();
                int8flag = false;
            }
            sett2val(40);
            setspkrt2();
        }

        public static void initsound()
        {
            settimer2(40);
            setspkrt2();
            settimer0(0);
            wavetype = 2;
            t0val = 12000;
            musvol = 8;
            t2val = 40;
            soundt0flag = true;
            sndflag = true;
            spkrmode = 0;
            int8flag = false;
            setsoundt2();
            soundstop();
            setupsound();
            timerrate = 0x4000;
            settimer0(0x4000);
            randvs = getlrt();
        }

        public static void s0killsound()
        {
            setsoundt2();
            timer2(40);
            stopint8();
        }

        public static void s0setupsound()
        {
            inittimer();
            curtime = 0;
            startint8();
        }

        public static void s0fillbuffer()
        {
        }
    }
}