/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

namespace Digger.Net
{
    public class Sound
    {
        private const int SOUND_BUFFER_SIZE = 4096;
        private const int SAMPLE_RATE = 44100;

        public int volume = 0;
        public bool isSoundEnabled = true;
        public bool isMusicEnabled = true;

        private int wavetype = 0, musvol = 0;
        private int spkrmode = 0, timerrate = 0x7d0;
        private ushort timercount = 0, t2val = 0, t0val = 0;
        private int pulsewidth = 1;
        private short nljpointer = 0;
        private short nljnoteduration = 0;
        private byte timerclock = 0;
        private bool sndflag = false, soundpausedflag = false;
        private bool soundlevdoneflag = false;
        private bool soundwobbleflag = false;
        private short soundwobblen = 0;
        private int soundfirew = 0;
        private readonly bool[] soundfireflag = new bool[Const.FIREBALLS];
        private readonly bool[] sff = new bool[Const.FIREBALLS];
        private readonly ushort[] soundfirevalue = new ushort[Const.FIREBALLS];
        private readonly ushort[] soundfiren = new ushort[Const.FIREBALLS];
        private readonly bool[] soundexplodeflag = new bool[Const.FIREBALLS];
        private readonly bool[] sef = new bool[Const.FIREBALLS];
        private readonly ushort[] soundexplodevalue = new ushort[Const.FIREBALLS];
        private readonly ushort[] soundexplodeduration = new ushort[Const.FIREBALLS];
        private int soundexplodew = 0;
        private bool soundbonusflag = false;
        private short soundbonusn = 0;
        private bool soundemeraldflag = false;
        private ushort soundemeraldduration, emerfreq, soundemeraldn;
        private bool soundgoldflag = false, soundgoldf = false;
        private ushort soundgoldvalue1, soundgoldvalue2, soundgoldduration;
        private bool soundddieflag = false;
        private ushort soundddien, soundddievalue;
        private bool sound1upflag = false;
        private short sound1upduration = 0;
        private bool musicplaying = false;
        private short musicp = 0, tuneno = 0, noteduration = 0, notevalue = 0, musicmaxvol = 0,
              musicattackrate = 0, musicsustainlevel = 0, musicdecayrate = 0, musicnotewidth = 0,
              musicreleaserate = 0, musicstage = 0, musicn = 0;
        private bool soundeatmflag = false;
        private ushort soundeatmvalue, soundeatmduration, soundeatmn;
        private bool soundfallflag = false, soundfallf = false;
        private ushort soundfallvalue, soundfalln = 0;
        private bool soundbreakflag = false;
        private ushort soundbreakduration = 0, soundbreakvalue = 0;
        private bool soundt0flag = false;
        private bool int8flag = false;

        private const int MIN_SAMP = 0;
        private const int MAX_SAMP = 0xff;
        /* The function which empties the circular buffer should get samples from
           buffer[firsts] and then do firsts=(firsts+1)&(size-1); This function is
           responsible for incrementing first samprate times per second (on average)
           (if it's a little out, the sound will simply run too fast or too slow). It
           must not take more than (last-firsts-1)&(size-1) samples at once, or the
           sound will break up.

           If DMA is used, doubling the buffer so the data is always continguous, and
           giving half of the buffer at once to the DMA driver may be a good idea. */

        private byte[] buffer;
        private ushort size;           /* data available to output device */
        private uint randvs = 0;

        private int rate;
        private ushort t0rate, t2rate, t2new, t0v, t2v;
        private short i8pulse = 0;
        private bool t2f = false, t2sw, i8flag = false;
        private readonly byte[] lut = new byte[257];
        private readonly ushort[] pwlut = new ushort[51];

        private readonly Game game;
        private readonly SDL_Sound device = new SDL_Sound();

        public Sound(Game game)
        {
            this.game = game;
        }

        public uint Randnos(int n)
        {
            randvs = randvs * 0x15a4e35 + 1;
            return (uint)((randvs & 0x7fffffff) % n);
        }

        public void SetT2Val(ushort t2v)
        {
            if (sndflag)
                Timer2(t2v);
        }

        public void SoundInt()
        {
            timerclock++;
            if (isSoundEnabled && !sndflag)
                sndflag = isMusicEnabled = true;
            if (!isSoundEnabled && sndflag)
            {
                sndflag = false;
                Timer2(40);
                SetSoundT2();
                SoundOff();
            }
            if (sndflag && !soundpausedflag)
            {
                t0val = 0x7d00;
                t2val = 40;
                if (isMusicEnabled)
                    MusicUpdate();
                soundemeraldupdate();
                SoundWobbleUpdate();
                SoundDiggerDieUpdate();
                SoundBreakUpdate();
                SoundGoldUpdate();
                SoundExplodeUpdate();
                SoundFireUpdate();
                SoundEatMonsterUpdate();
                SoundFallUpdate();
                Sound1UpUpdate();
                SoundBonusUpdate();
                if (t0val == 0x7d00 || t2val != 40)
                    SetSoundT2();
                else
                {
                    SetSoundMode();
                    SetT0();
                }
                SetT2Val(t2val);
            }
        }

        public void StopSound()
        {
            int i;
            SoundFallOff();
            SoundWobbleOff();
            for (i = 0; i < Const.FIREBALLS; i++)
                SoundFireOff(i);
            MusicOff();
            SoundBonusOff();
            for (i = 0; i < Const.FIREBALLS; i++)
                SoundExplodeOff(i);
            SoundBreakOff();
            SoundEmeraldOff();
            SoundGoldOff();
            SoundEatMonsterOff();
            SoundDiggerDieOff();
            Sound1UpOff();
        }

        public void SoundLevelDone(Input input)
        {
            short timer = 0;
            StopSound();
            nljpointer = 0;
            nljnoteduration = 20;
            soundlevdoneflag = soundpausedflag = true;
            game.timer.FrameTime /= 5;
            while (soundlevdoneflag && !game.isGameCycleEnded)
            {
                if (!device.IsWaveDeviceAvailable)
                    soundlevdoneflag = false;

                game.timer.SyncFrame();	/* Let some CPU time go away */
                SoundInt();

                if (timerclock == timer)
                    continue;
                SoundLevdoneUpdate();
                input.CheckKeyBuffer();
                timer = timerclock;
            }
            game.timer.FrameTime *= 5;
            SoundLevelDoneOff();
        }

        public void SoundLevelDoneOff()
        {
            soundlevdoneflag = soundpausedflag = false;
        }

        public void SoundLevdoneUpdate()
        {
            if (sndflag)
            {
                if (nljpointer < 11)
                    t2val = Sounds.NewLevelJingle[nljpointer];
                t0val = (ushort)(t2val + 35);
                musvol = 50;
                SetSoundMode();
                SetT0();
                SetT2Val(t2val);
                if (nljnoteduration > 0)
                    nljnoteduration--;
                else
                {
                    nljnoteduration = 20;
                    nljpointer++;
                    if (nljpointer > 10)
                        SoundLevelDoneOff();
                }
            }
            else
                soundlevdoneflag = false;
        }

        public void SoundFall()
        {
            soundfallvalue = 1000;
            soundfallflag = true;
        }

        public void SoundFallOff()
        {
            soundfallflag = false;
            soundfalln = 0;
        }

        public void SoundFallUpdate()
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

        public void SoundBreak()
        {
            soundbreakduration = 3;
            if (soundbreakvalue < 15000)
                soundbreakvalue = 15000;
            soundbreakflag = true;
        }

        public void SoundBreakOff()
        {
            soundbreakflag = false;
        }

        public void SoundBreakUpdate()
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

        public void SoundWobble()
        {
            soundwobbleflag = true;
        }

        public void SoundWobbleOff()
        {
            soundwobbleflag = false;
            soundwobblen = 0;
        }

        public void SoundWobbleUpdate()
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

        public void SoundFire(int n)
        {
            soundfirevalue[n] = 500;
            soundfireflag[n] = true;
        }

        public void SoundFireOff(int n)
        {
            soundfireflag[n] = false;
            soundfiren[n] = 0;
        }

        public void SoundFireUpdate()
        {
            int n;
            bool f = false;
            for (n = 0; n < Const.FIREBALLS; n++)
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
                            SoundFireOff(n);
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
                    if (soundfirew == Const.FIREBALLS)
                        soundfirew = 0;
                } while (!sff[n]);
                t2val = (ushort)(soundfirevalue[n] + Randnos(soundfirevalue[n] >> 3));
            }
        }

        public void SoundExplode(int n)
        {
            soundexplodevalue[n] = 1500;
            soundexplodeduration[n] = 10;
            soundexplodeflag[n] = true;
            SoundFireOff(n);
        }

        public void SoundExplodeOff(int n)
        {
            soundexplodeflag[n] = false;
        }

        public void SoundExplodeUpdate()
        {
            int n;
            bool f = false;
            for (n = 0; n < Const.FIREBALLS; n++)
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
                    if (soundexplodew == Const.FIREBALLS)
                        soundexplodew = 0;
                } while (!sef[n]);
                t2val = soundexplodevalue[n];
            }
        }

        public void SoundBonus()
        {
            soundbonusflag = true;
        }

        public void SoundBonusOff()
        {
            soundbonusflag = false;
            soundbonusn = 0;
        }

        public void SoundBonusUpdate()
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

        public void SoundEmerald(int n)
        {
            emerfreq = Sounds.emfreqs[n];
            soundemeraldduration = 7;
            soundemeraldn = 0;
            soundemeraldflag = true;
        }

        public void SoundEmeraldOff()
        {
            soundemeraldflag = false;
        }

        public void soundemeraldupdate()
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
                    SoundEmeraldOff();
            }
        }

        public void SoundGold()
        {
            soundgoldvalue1 = 500;
            soundgoldvalue2 = 4000;
            soundgoldduration = 30;
            soundgoldf = false;
            soundgoldflag = true;
        }

        public void SoundGoldOff()
        {
            soundgoldflag = false;
        }

        public void SoundGoldUpdate()
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

        public void SoundEatMonster()
        {
            soundeatmduration = 20;
            soundeatmn = 3;
            soundeatmvalue = 2000;
            soundeatmflag = true;
        }

        public void SoundEatMonsterOff()
        {
            soundeatmflag = false;
        }

        public void SoundEatMonsterUpdate()
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

        public void SoundDiggerDie()
        {
            soundddien = 0;
            soundddievalue = 20000;
            soundddieflag = true;
        }

        public void SoundDiggerDieOff()
        {
            soundddieflag = false;
        }

        public void SoundDiggerDieUpdate()
        {
            if (soundddieflag)
            {
                soundddien++;
                if (soundddien == 1)
                    MusicOff();
                if (soundddien >= 1 && soundddien <= 10)
                    soundddievalue = (ushort)(20000 - soundddien * 1000);
                if (soundddien > 10)
                    soundddievalue += 500;
                if (soundddievalue > 30000)
                    SoundDiggerDieOff();
                t2val = soundddievalue;
            }
        }

        public void Sound1Up()
        {
            sound1upduration = 96;
            sound1upflag = true;
        }

        public void Sound1UpOff()
        {
            sound1upflag = false;
        }

        public void Sound1UpUpdate()
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

        public void Music(short tune)
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
                SoundDiggerDieOff();
        }

        public void MusicOff()
        {
            musicplaying = false;
            musicp = 0;
        }

        public void MusicUpdate()
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
                        noteduration = (short)(Sounds.bonusjingle[musicp + 1] * 3);
                        musicnotewidth = (short)(noteduration - 3);
                        notevalue = Sounds.bonusjingle[musicp];
                        musicp += 2;
                        if (Sounds.bonusjingle[musicp] == 0x7d64)
                            musicp = 0;
                        break;
                    case 1:
                        noteduration = (short)(Sounds.backgjingle[musicp + 1] * 6);
                        musicnotewidth = 12;
                        notevalue = Sounds.backgjingle[musicp];
                        musicp += 2;
                        if (Sounds.backgjingle[musicp] == 0x7d64)
                            musicp = 0;
                        break;
                    case 2:
                        noteduration = (short)(Sounds.dirge[musicp + 1] * 10);
                        musicnotewidth = (short)(noteduration - 10);
                        notevalue = Sounds.dirge[musicp];
                        musicp += 2;
                        if (Sounds.dirge[musicp] == 0x7d64)
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


        public void SoundPause()
        {
            soundpausedflag = true;
            device.DisableSound();
        }

        public void SoundPauseOff()
        {
            soundpausedflag = false;
            device.EnableSound();
        }

        public void SetT0()
        {
            if (sndflag)
            {
                Timer2(t2val);
                if (t0val < 1000 && (wavetype == 1 || wavetype == 2))
                    t0val = 1000;
                Timer0(t0val);
                timerrate = t0val;
                if (musvol < 1)
                    musvol = 1;
                if (musvol > 50)
                    musvol = 50;
                pulsewidth = musvol * volume;
                SetSoundMode();
            }
        }

        public void SetSoundT2()
        {
            if (soundt0flag)
            {
                spkrmode = 0;
                soundt0flag = false;
                SetSpeakerT2();
            }
        }

        public void SetSoundMode()
        {
            spkrmode = wavetype;
            if (!soundt0flag && sndflag)
            {
                soundt0flag = true;
                SetSpeakerT2();
            }
        }

        private void StartInt8()
        {
            if (!int8flag)
            {
                timerrate = 0x4000;
                SetTimer0(0x4000);
                int8flag = true;
            }
        }

        private void StopInt8()
        {
            SetTimer0(0);
            if (int8flag)
                int8flag = false;

            SetT2Val(40);
            SetSpeakerT2();
        }

        public void Initialize()
        {
            SetTimer2(40);
            SetSpeakerT2();
            SetTimer0(0);
            wavetype = 2;
            t0val = 12000;
            musvol = 8;
            t2val = 40;
            soundt0flag = true;
            sndflag = true;
            spkrmode = 0;
            int8flag = false;
            SetSoundT2();
            StopSound();
            SetupSound();
            timerrate = 0x4000;
            SetTimer0(0x4000);
            randvs = 0;
        }

        public void KillSound()
        {
            SetSoundT2();
            Timer2(40);
            StopInt8();
            SetSoundT2();
        }

        public void SetupSound()
        {
            game.currentTime = 0;
            StartInt8();
        }

        /* Initialise circular buffer and PC speaker emulator

           bufsize = buffer size in samples
           samprate = play rate in Hz

           samprate is directly proportional to the sound quality. This should be the
           highest value the hardware can support without slowing down the program too
           much. Ensure 0x1234<samprate<=0x1234dd and that samprate is a factor of
           0x1234dd (or you won't get the rate you want). For example, a value of
           44100 equates to about 44192Hz (a .2% difference - negligable unless you're
           trying to harmonize with a computer running at a different rate, or another
           musical instrument...)

           The lag time is bufsize/samprate seconds. This should be the smallest value
           which does not make the sound break up. There may also be DMA considerations
           to take into account. bufsize should also be a power of 2.
        */
        public void SoundInitGlobal()
        {
            device.SetDevice(SAMPLE_RATE, SOUND_BUFFER_SIZE, GetSample);
            size = SOUND_BUFFER_SIZE << 1;
            buffer = new byte[size];
            rate = 0x1234dd / SAMPLE_RATE;
            t2sw = false;     /* As it should be left */
            for (int i = 0; i <= rate; i++)
                lut[i] = (byte)(MIN_SAMP + (i * (MAX_SAMP - MIN_SAMP)) / rate);
            for (int i = 1; i <= 50; i++)
                pwlut[i] = (ushort)((16 + i * 18) >> 2); /* Counted timer ticks in original */
        }

        /* WARNING: Read only code ahead. Unless you're seriously into how the PC
           speaker and Digger's original low-level sound routines work, you shouldn't
           try to mess with, or even understand, the following. I don't understand most
           of it myself, and I wrote it. */

        public void SetTimer2(ushort t2)
        {
            if (t2 == 40)
                t2 = (ushort)rate;   /* Otherwise aliasing would cause noise artifacts */
            t2 >>= 1;
            t2v = t2new = t2;
        }

        public void SoundOff()
        {
            t2sw = false;
        }

        public void SetSpeakerT2()
        {
            t2sw = true;
        }

        public void SetTimer0(ushort t0)
        {
            t0v = t0rate = t0;
        }

        public void Timer0(ushort t0)
        {
            t0rate = t0;
        }

        public void Timer2(ushort t2)
        {
            if (t2 == 40)
                t2 = (ushort)rate;    /* Otherwise aliasing would cause noise artifacts */
            t2 >>= 1;
            t2new = t2rate = t2;
            t2v = t2rate;
        }

        private bool AddCarry(ref ushort dest, ushort add)
        {
            dest += add;
            return dest < add;
        }

        private bool SubCarry(ref ushort dest, ushort sub)
        {
            dest -= sub;
            return dest >= (ushort)-sub;
        }

        /* This function is the workhorse.
           It emulates the functionality of:
            * the 8253 Programmable Interval Timer
            * the PC speaker hardware
            * the IRQ0 timer interrupt which Digger reprograms
           It averages the speaker values over the entire time interval to get the
           sample.
           Despite its complexity, it runs pretty fast, since most of the time, it
           doesn't actually do very much, and when it does stuff, it uses look-up
           tables.
           There are probably fencepost errors but I challenge anyone to detect these
           audibly.
           Some would just calculate each bit separately and add them up, but there
           are 1,193,181 bits to add up per second, so you'd need a fast PC. This may
           be a little more complicated, but its much faster.
        */
        public byte GetSample()
        {
            bool f = false, t2sw0;
            ushort spkrt2 = 0, noi8 = 0, complicate = 0, not2 = 0;

            if (SubCarry(ref t2v, (ushort)rate))
            {
                not2 = (ushort)(t2v + rate); /* Amount of time that went by before change */
                if (t2f)
                {
                    spkrt2 = (ushort)-t2v; /* MIN_SAMPs at beginning */
                    t2rate = t2new;
                    if (t2rate == (rate >> 1))
                        t2v = t2rate;
                }
                else /* MIN_SAMPs at end */
                    spkrt2 = (ushort)(t2v + rate);
                t2v += t2rate;
                if (t2rate == (rate >> 1))
                    t2v = t2rate;
                else
                    t2f = !t2f;
                complicate |= 1;
            }

            if (SubCarry(ref t0v, (ushort)rate))
            { /* Effectively using mode 2 here */
                i8flag = true;
                noi8 = (ushort)(t0v + rate); /* Amount of time that went by before interrupt */
                t0v += t0rate;
                complicate |= 2;
            }

            t2sw0 = t2sw;

            if (i8flag && i8pulse <= 0)
            {
                f = true;
                if (spkrmode != 0)
                {
                    if (spkrmode != 1)
                        t2sw = !t2sw;
                    else
                    {
                        i8pulse = (short)pwlut[pulsewidth];
                        t2sw = true;
                        f = false;
                    }
                }
            }

            if (i8pulse > 0)
            {
                complicate |= 4;
                i8pulse -= (short)rate;
                if (i8pulse <= 0)
                {
                    complicate |= 8;
                    t2sw = false;
                    i8flag = true;
                    f = true;
                }
            }

            if (f)
            {
                if (AddCarry(ref timercount, (ushort)timerrate))
                {
                    SoundInt(); /* Update music and sound effects 72.8 Hz */
                    timercount -= 0x4000;
                }
                i8flag = false;
            }

            if ((complicate & 1) == 0 && t2f)
                return MIN_SAMP;

            /* 12 unique cases, no break statements!
               No more than about 6 of these lines are executed on any single call. */

            switch (complicate)
            {
                case 2: /* Int8 happened */
                    if (t2sw != t2sw0)
                    {
                        if (t2sw) /* <==> !t2sw0 */
                            return lut[rate - noi8];
                        return lut[noi8];
                    }
                    goto case 0;
                /* Fall through */
                case 0: /* Nothing happened! */
                    if (!t2sw)
                        return MIN_SAMP;
                    goto case 4;
                /* Fall through */
                case 4: /* Int8 is pulsing => t2sw */
                    return MAX_SAMP;
                case 1: /* The t2 wave changed */
                    if (!t2sw)
                        return MIN_SAMP;
                    goto case 5;
                /* Fall through */
                case 5: /* The t2 wave changed and Int8 is pulsing => t2sw */
                    return lut[spkrt2];
                case 3: /* Int8 happened and t2 wave changed */
                    if (!t2sw0 && !t2sw)
                        return MIN_SAMP;    /* both parts are off */
                    if (t2sw0 && t2sw)
                        return lut[spkrt2]; /* both parts are on */
                    if (not2 < noi8)  /* t2 happened first */
                        if (t2sw0) /* "on" part is before i8 */
                            if (t2f)
                                return lut[spkrt2]; /* MIN_SAMPs at end */
                            else
                                return lut[spkrt2 - (rate - noi8)]; /* MIN_SAMPs at beginning */
                        else      /* "on" part is after i8 => constant */
                          if (t2f)
                            return MIN_SAMP; /* MIN_SAMPs at end */
                        else
                            return lut[rate - noi8]; /* MIN_SAMPs at beginning */
                    else /* i8 happened first */
                      if (t2sw0) /* "on" part is before i8 => constant */
                        if (t2f)
                            return MIN_SAMP; /* MIN_SAMPs at beginning */
                        else
                            return lut[noi8]; /* MIN_SAMPs at end */
                    else       /* "on" part is after i8 */
                        if (t2f)
                        return lut[spkrt2]; /* MIN_SAMPs at beginning */
                    else if (spkrt2 > noi8)
                        return lut[spkrt2 - noi8]; /* MIN_SAMPs at end */
                    else
                        return MIN_SAMP;
                case 6: /* The Int8 pulse started */
                    if (t2sw0)
                        return MAX_SAMP;
                    return lut[rate - noi8];
                case 7: /* The Int8 pulse started and the t2 wave changed */
                    if (t2sw0)
                        return lut[spkrt2];
                    if (not2 < noi8)  /* t2 happened first */
                        if (t2f)
                            return MIN_SAMP; /* MIN_SAMPs at end */
                        else
                            return lut[rate - noi8]; /* MIN_SAMPs at beginning */
                    else /* i8 happened first */
                      if (t2f)
                        return lut[spkrt2]; /* MIN_SAMPs at beginning */
                    else if (spkrt2 > noi8)
                        return lut[spkrt2 - noi8]; /* MIN_SAMPs at end */
                    else
                        return lut[spkrt2];
                case 12: /* The Int8 pulse stopped */
                    if (t2sw)
                        return MAX_SAMP;
                    return lut[i8pulse + rate];
                case 13: /* The Int8 pulse stopped and the t2 wave changed */
                    if (t2sw)
                        return lut[spkrt2];
                    if (not2 < i8pulse + rate) /* t2 happened first */
                        if (t2f)
                            if (spkrt2 + i8pulse > 0)
                                return lut[spkrt2 + i8pulse]; /* MIN_SAMPs at beginning */
                            else
                                return lut[spkrt2];
                        else
                            return lut[spkrt2];         /* MIN_SAMPs at end */
                    else /* i8pulse ended first */
                      if (t2f)
                        return MIN_SAMP; /* MIN_SAMPs at beginning */
                    else
                        return lut[i8pulse + rate];
                case 14: /* The Int8 pulse started and stopped in the same sample */
                    if (t2sw0)
                        if (t2sw)
                            return MAX_SAMP;
                        else
                            return lut[noi8 + i8pulse + rate];
                    else
                      if (t2sw)
                        return lut[rate - noi8];
                    else
                        return lut[i8pulse + rate];
                case 15: /* Everything happened at once */
                    if (not2 < noi8) /* First subcase: t2 happens before pulse */
                        if (t2f) /* MIN_SAMPs at beginning */
                            if (t2sw0)
                                if (t2sw)
                                    return lut[spkrt2];
                                else
                                    return lut[spkrt2 + noi8 + i8pulse];
                            else
                              if (t2sw)
                                return lut[rate - noi8];
                            else
                                return lut[i8pulse + rate];
                        else /* MIN_SAMPs at end */
                          if (t2sw0) /* No need to test t2sw */
                            return lut[spkrt2];
                        else
                            return MIN_SAMP;
                    else
                      if (not2 < rate + noi8 + i8pulse) /* Subcase 2: t2 happens during pulse */
                        if (t2f) /* MIN_SAMPs at beginning */
                            if (t2sw) /* No need to test t2sw0 */
                                return lut[spkrt2];
                            else
                                return lut[spkrt2 + noi8 + i8pulse];
                        else /* MIN_SAMPs at end */
                          if (t2sw0) /* No need to test t2sw */
                            return lut[spkrt2];
                        else
                            return lut[spkrt2 - noi8];
                    else /* Third subcase: t2 happens after pulse */
                        if (t2f) /* MIN_SAMPs at beginning */
                        if (t2sw) /* No need to test t2sw0 */
                            return lut[spkrt2];
                        else
                            return MIN_SAMP;
                    else /* MIN_SAMPs at end */
                          if (t2sw0)
                        if (t2sw)
                            return lut[spkrt2];
                        else
                            return lut[noi8 + i8pulse + rate];
                    else
                            if (t2sw)
                        return lut[spkrt2 - noi8];
                    else
                        return lut[i8pulse + rate];
            }
            return MIN_SAMP; /* This should never happen */
        }
    }
}