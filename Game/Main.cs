/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;
using System.IO;
using System.Text;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public const int DIR_NONE = -1;
        public const int DIR_RIGHT = 0;
        public const int DIR_UP = 2;
        public const int DIR_LEFT = 4;
        public const int DIR_DOWN = 6;

        public const int TYPES = 5;

        public const int BONUSES = 1;
        public const int BAGS = 7;
        public const int MONSTERS = 6;
        public const int FIREBALLS = DIGGERS;
        public const int DIGGERS = 2;
        public const int SPRITES = (BONUSES + BAGS + MONSTERS + FIREBALLS + DIGGERS);

        /* Sprite order is figured out here. By LAST I mean last+1. */
        public const int FIRSTBONUS = 0;
        public const int LASTBONUS = (FIRSTBONUS + BONUSES);
        public const int FIRSTBAG = LASTBONUS;
        public const int LASTBAG = (FIRSTBAG + BAGS);
        public const int FIRSTMONSTER = LASTBAG;
        public const int LASTMONSTER = (FIRSTMONSTER + MONSTERS);
        public const int FIRSTFIREBALL = LASTMONSTER;
        public const int LASTFIREBALL = (FIRSTFIREBALL + FIREBALLS);
        public const int FIRSTDIGGER = LASTFIREBALL;
        public const int LASTDIGGER = (FIRSTDIGGER + DIGGERS);

        public const int MWIDTH = 15;
        public const int MHEIGHT = 10;
        public const int MSIZE = MWIDTH * MHEIGHT;

        public const string INI_GAME_SETTINGS = "Game";
        public const string INI_GRAPHICS_SETTINGS = "Graphics";
        public const string INI_SOUND_SETTINGS = "Sound";
        public const string INI_KEY_SETTINGS = "Keys";

        /* using lesser buffer size will break ie. alsa on linux, no reason to use
         * lesser size anyways...
         */
        public const int DEFAULT_BUFFER = 2048;
        public const int DEF_SND_DEV = 0;
        public const string ININAME = "ini";

        /// <summary>
        /// Version string:
        /// First word: your initials if you have changed anything.
        /// Second word: platform. 
        /// Third word: compilation date in yyyymmdd format.
        /// </summary>
        public const string DIGGER_VERSION = "MS SDL 20180419";

        /* global variables */
        public static string pldispbuf;
        public static int curplayer = 0, nplayers = 1, penalty = 0, diggers = 1, startlev = 1;
        public static bool unlimlives = false, gauntlet = false, timeout = false, synchvid = false;
        public static uint gtime = 0;

        public class game_data
        {
            public int level = 1;
            public bool levdone;
        }

        public static game_data[] gamedat = { new game_data(), new game_data() };

        public static bool levnotdrawn = false, alldead = false;
        public static bool maininited, started;

        public static string levfname;
        public static bool levfflag = false;

        public static string[,] leveldat = {
            { "S   B     HHHHS",
              "V  CC  C  V B  ",
              "VB CC  C  V    ",
              "V  CCB CB V CCC",
              "V  CC  C  V CCC",
              "HH CC  C  V CCC",
              " V    B B V    ",
              " HHHH     V    ",
              "C   V     V   C",
              "CC  HHHHHHH  CC" },
            { "SHHHHH  B B  HS",
              " CC  V       V ",
              " CC  V CCCCC V ",
              "BCCB V CCCCC V ",
              "CCCC V       V ",
              "CCCC V B  HHHH ",
              " CC  V CC V    ",
              " BB  VCCCCV CC ",
              "C    V CC V CC ",
              "CC   HHHHHH    "},
            { "SHHHHB B BHHHHS",
              "CC  V C C V BB ",
              "C   V C C V CC ",
              " BB V C C VCCCC",
              "CCCCV C C VCCCC",
              "CCCCHHHHHHH CC ",
              " CC  C V C  CC ",
              " CC  C V C     ",
              "C    C V C    C",
              "CC   C H C   CC"},
            { "SHBCCCCBCCCCBHS",
              "CV  CCCCCCC  VC",
              "CHHH CCCCC HHHC",
              "C  V  CCC  V  C",
              "   HHH C HHH   ",
              "  B  V B V  B  ",
              "  C  VCCCV  C  ",
              " CCC HHHHH CCC ",
              "CCCCC CVC CCCCC",
              "CCCCC CHC CCCCC"},
            { "SHHHHHHHHHHHHHS",
              "VBCCCCBVCCCCCCV",
              "VCCCCCCV CCBC V",
              "V CCCC VCCBCCCV",
              "VCCCCCCV CCCC V",
              "V CCCC VBCCCCCV",
              "VCCBCCCV CCCC V",
              "V CCBC VCCCCCCV",
              "VCCCCCCVCCCCCCV",
              "HHHHHHHHHHHHHHH"},
            { "SHHHHHHHHHHHHHS",
              "VCBCCV V VCCBCV",
              "VCCC VBVBV CCCV",
              "VCCCHH V HHCCCV",
              "VCC V CVC V CCV",
              "VCCHH CVC HHCCV",
              "VC V CCVCC V CV",
              "VCHHBCCVCCBHHCV",
              "VCVCCCCVCCCCVCV",
              "HHHHHHHHHHHHHHH"},
            { "SHCCCCCVCCCCCHS",
              " VCBCBCVCBCBCV ",
              "BVCCCCCVCCCCCVB",
              "CHHCCCCVCCCCHHC",
              "CCV CCCVCCC VCC",
              "CCHHHCCVCCHHHCC",
              "CCCCV CVC VCCCC",
              "CCCCHH V HHCCCC",
              "CCCCCV V VCCCCC",
              "CCCCCHHHHHCCCCC"},
            { "HHHHHHHHHHHHHHS",
              "V CCBCCCCCBCC V",
              "HHHCCCCBCCCCHHH",
              "VBV CCCCCCC VBV",
              "VCHHHCCCCCHHHCV",
              "VCCBV CCC VBCCV",
              "VCCCHHHCHHHCCCV",
              "VCCCC V V CCCCV",
              "VCCCCCV VCCCCCV",
              "HHHHHHHHHHHHHHH" }};

        public static char getlevch(int x, int y, int l)
        {
            if ((l == 3 || l == 4) && !levfflag && diggers == 2 && y == 9 && (x == 6 || x == 8))
                return 'H';
            return leveldat[l - 1, y][x];
        }

        public static void game()
        {
            int t, c, i;
            bool flashplayer = false;
            if (gauntlet)
            {
                cgtime = gtime * 1193181;
                timeout = false;
            }
            initlives();
            alldead = false;
            ddap.clear();
            curplayer = 0;
            initlevel();
            curplayer = 1;
            initlevel();
            zeroscores();
            bonusvisible = true;
            if (nplayers == 2)
                flashplayer = true;
            curplayer = 0;
            while (getalllives() != 0 && !escape && !timeout)
            {
                while (!alldead && !escape && !timeout)
                {
                    initmbspr();

                    if (playing)
                        randv = playgetrand();
                    else
                        randv = getlrt();

                    recputrand(randv);
                    if (levnotdrawn)
                    {
                        levnotdrawn = false;
                        drawscreen(ddap);
                        if (flashplayer)
                        {
                            flashplayer = false;
                            pldispbuf = "PLAYER " + (curplayer == 0 ? "1" : "2");
                            cleartopline();
                            for (t = 0; t < 15; t++)
                            {
                                for (c = 1; c <= 3; c++)
                                {
                                    outtext(ddap, pldispbuf, 108, 0, c);
                                    writecurscore(ddap, c);
                                    newframe();
                                    if (escape)
                                        return;
                                }
                            }
                            drawscores(ddap);
                            for (i = 0; i < diggers; i++)
                                addscore(ddap, i, 0);
                        }
                    }
                    else
                        initchars();

                    erasetext(ddap, 8, 108, 0, 3);
                    initscores(ddap);
                    drawlives(ddap);
                    music(1);

                    flushkeybuf();
                    for (i = 0; i < diggers; i++)
                        readdirect(i);

                    while (!alldead && !gamedat[curplayer].levdone && !escape && !timeout)
                    {
                        penalty = 0;
                        dodigger(ddap);
                        domonsters(ddap);
                        dobags(ddap);
                        if (penalty > 8)
                            incmont(penalty - 8);
                        testpause();
                        checklevdone();
                    }
                    erasediggers();
                    musicoff();
                    t = 20;
                    while ((getnmovingbags() != 0 || t != 0) && !escape && !timeout)
                    {
                        if (t != 0)
                            t--;
                        penalty = 0;
                        dobags(ddap);
                        dodigger(ddap);
                        domonsters(ddap);
                        if (penalty < 8)
                            t = 0;
                    }
                    soundstop();
                    for (i = 0; i < diggers; i++)
                        killfire(i);
                    erasebonus(ddap);
                    cleanupbags();
                    savefield();
                    erasemonsters();
                    recputeol();
                    if (playing)
                        playskipeol();
                    if (escape)
                        recputeog();
                    if (gamedat[curplayer].levdone)
                        soundlevdone();
                    if (countem() == 0 || gamedat[curplayer].levdone)
                    {
                        for (i = curplayer; i < diggers + curplayer; i++)
                            if (getlives(i) > 0 && !digalive(i))
                                declife(i);
                        drawlives(ddap);
                        gamedat[curplayer].level++;
                        if (gamedat[curplayer].level > 1000)
                            gamedat[curplayer].level = 1000;
                        initlevel();
                    }
                    else
                      if (alldead)
                    {
                        for (i = curplayer; i < curplayer + diggers; i++)
                            if (getlives(i) > 0)
                                declife(i);
                        drawlives(ddap);
                    }
                    if ((alldead && getalllives() == 0 && !gauntlet && !escape) || timeout)
                        endofgame(ddap);
                }
                alldead = false;
                if (nplayers == 2 && getlives(1 - curplayer) != 0)
                {
                    curplayer = 1 - curplayer;
                    flashplayer = levnotdrawn = true;
                }
            }
        }

        static bool quiet = false;
        static ushort sound_rate, sound_length;

        public static void maininit()
        {
            if (maininited)
                return;

            for (int i = 0; i < DIGGERS; ++i)
                digdat[i] = new digger_struct();
            
            calibrate();
            ddap.init();
            ddap.pal(0);
            setretr(true);
            initkeyb();
            detectjoy();
            initsound();
            recstart();

            maininited = true;
        }

        public static int mainprog()
        {
            int frame, t;
            monster_obj nobbin, hobbin;
            digger_obj odigger = null;
            obj_position newpos;
            loadscores();
            escape = false;
            nobbin = null;
            hobbin = null;
            do
            {
                soundstop();
                creatembspr();
                detectjoy();
                ddap.clear();
                ddap.title();
                outtext(ddap, "D I G G E R", 100, 0, 3);
                shownplayers();
                showtable(ddap);
                started = false;
                frame = 0;
                newframe();
                teststart();
                while (!started)
                {
                    started = teststart();
                    if (mode_change)
                    {
                        switchnplayers();
                        shownplayers();
                        mode_change = false;
                    }
                    if (frame == 0)
                        for (t = 54; t < 174; t += 12)
                            erasetext(ddap, 12, 164, t, 0);
                    if (frame == 50)
                    {
                        nobbin = new monster_obj(0, MON_NOBBIN, DIR_LEFT, 292, 63);
                        nobbin.put();
                    }
                    if (frame > 50 && frame <= 77)
                    {
                        newpos = nobbin.getpos();
                        newpos.x -= 4;
                        if (frame == 77)
                        {
                            newpos.dir = DIR_RIGHT;
                        }
                        nobbin.setpos(newpos);
                    }
                    if (frame > 50)
                    {
                        nobbin.animate();
                    }

                    if (frame == 83)
                        outtext(ddap, "NOBBIN", 216, 64, 2);
                    if (frame == 90)
                    {
                        hobbin = new monster_obj(1, MON_NOBBIN, DIR_LEFT, 292, 82);
                        hobbin.put();
                    }
                    if (frame > 90 && frame <= 117)
                    {
                        newpos = hobbin.getpos();
                        newpos.x -= 4;
                        if (frame == 117)
                        {
                            newpos.dir = DIR_RIGHT;
                        }
                        hobbin.setpos(newpos);
                    }
                    if (frame == 100)
                    {
                        hobbin.mutate();
                    }
                    if (frame > 90)
                    {
                        hobbin.animate();
                    }
                    if (frame == 123)
                        outtext(ddap, "HOBBIN", 216, 83, 2);
                    if (frame == 130)
                    {
                        odigger = new digger_obj(0, DIR_LEFT, 292, 101);
                        odigger.put();
                    }
                    if (frame > 130 && frame <= 157)
                    {
                        odigger.x -= 4;
                    }
                    if (frame > 157)
                    {
                        odigger.dir = DIR_RIGHT;
                    }
                    if (frame >= 130)
                    {
                        odigger.animate();
                    }
                    if (frame == 163)
                        outtext(ddap, "DIGGER", 216, 102, 2);
                    if (frame == 178)
                    {
                        movedrawspr(FIRSTBAG, 184, 120);
                        drawgold(0, 0, 184, 120);
                    }
                    if (frame == 183)
                        outtext(ddap, "GOLD", 216, 121, 2);
                    if (frame == 198)
                        drawemerald(184, 141);
                    if (frame == 203)
                        outtext(ddap, "EMERALD", 216, 140, 2);
                    if (frame == 218)
                        drawbonus(184, 158);
                    if (frame == 223)
                        outtext(ddap, "BONUS", 216, 159, 2);
                    if (frame == 235)
                    {
                        nobbin.damage();
                    }
                    if (frame == 239)
                    {
                        nobbin.kill();
                    }
                    if (frame == 242)
                    {
                        hobbin.damage();
                    }
                    if (frame == 246)
                    {
                        hobbin.kill();
                    }
                    newframe();
                    frame++;
                    if (frame > 250)
                        frame = 0;
                }
                if (savedrf)
                {
                    if (gotgame)
                    {
                        recsavedrf();
                        gotgame = false;
                    }
                    savedrf = false;
                    continue;
                }
                if (escape)
                    break;
                recinit();
                game();
                gotgame = true;
                if (gotname)
                {
                    recsavedrf();
                    gotgame = false;
                }
                savedrf = false;
                escape = false;
            } while (!escape);
            finish();
            return 0;
        }

        public static void finish()
        {
            killsound();
            soundoff();
            soundkillglob();
            restorekeyb();
            graphicsoff();
        }

        public struct label
        {
            public string text;
            public int xpos;

            public label(string text, int xpos)
            {
                this.text = text;
                this.xpos = xpos;
            }
        };

        public struct game_mode
        {
            public bool gauntlet;
            public int nplayers;
            public int diggers;
            public bool last;
            public label[] title;

            public game_mode(bool gauntlet, int nplayers, int diggers, bool last, label[] title)
            {
                this.gauntlet = gauntlet;
                this.nplayers = nplayers;
                this.diggers = diggers;
                this.last = last;
                this.title = title;
            }
        }

        public static game_mode[] possible_modes = {
            new game_mode(false, 1, 1, false, new label[]{ new label("ONE", 220), new label(" PLAYER ", 192)}),
            new game_mode(false, 2, 1, false, new label[]{ new label("TWO", 220), new label(" PLAYERS", 184)}),
            new game_mode(false, 2, 2, false, new label[]{ new label("TWO PLAYER", 180), new label( "SIMULTANEOUS", 170)}),
            new game_mode(true, 1, 1, false, new label[]{ new label("GAUNTLET", 192), new label( "MODE", 216)}),
            new game_mode(true, 1, 2, true, new label[]{ new label("TWO PLAYER", 180), new label( "GAUNTLET", 192)})
        };

        static int getnmode()
        {
            int i;

            for (i = 0; !possible_modes[i].last; i++)
            {
                if (possible_modes[i].gauntlet != gauntlet)
                    continue;
                if (possible_modes[i].nplayers != nplayers)
                    continue;
                if (possible_modes[i].diggers != diggers)
                    continue;
                break;
            }
            return i;
        }

        public static void shownplayers()
        {
            game_mode gmp;

            erasetext(ddap, 10, 180, 25, 3);
            erasetext(ddap, 12, 170, 39, 3);
            gmp = possible_modes[getnmode()];
            outtext(ddap, gmp.title[0].text, gmp.title[0].xpos, 25, 3);
            outtext(ddap, gmp.title[1].text, gmp.title[1].xpos, 39, 3);
        }

        public static int getalllives()
        {
            int t = 0, i;
            for (i = curplayer; i < diggers + curplayer; i++)
                t += getlives(i);
            return t;
        }

        public static void switchnplayers()
        {
            int i = getnmode();
            int j = possible_modes[i].last ? 0 : i + 1;
            gauntlet = possible_modes[j].gauntlet;
            nplayers = possible_modes[j].nplayers;
            diggers = possible_modes[j].diggers;
        }

        public static void initlevel()
        {
            gamedat[curplayer].levdone = false;
            makefield();
            makeemfield();
            initbags();
            levnotdrawn = true;
        }

        public static void drawscreen(digger_draw_api ddap)
        {
            creatembspr();
            drawstatics(ddap);
            drawbags();
            drawemeralds();
            initdigger();
            initmonsters();
        }

        public static void initchars()
        {
            initmbspr();
            initdigger();
            initmonsters();
        }

        public static void checklevdone()
        {
            if ((countem() == 0 || monleft() == 0) && isalive())
                gamedat[curplayer].levdone = true;
            else
                gamedat[curplayer].levdone = false;
        }

        public static void incpenalty()
        {
            penalty++;
        }

        public static void cleartopline()
        {
            erasetext(ddap, 26, 0, 0, 3);
            erasetext(ddap, 1, 308, 0, 3);
        }

        public static int levplan()
        {
            int l = levno();
            if (l > 8)
                l = (l & 3) + 5; /* Level plan: 12345678, 678, (5678) 247 times, 5 forever */
            return l;
        }

        public static int levof10()
        {
            if (gamedat[curplayer].level > 10)
                return 10;
            return gamedat[curplayer].level;
        }

        public static int levno()
        {
            return gamedat[curplayer].level;
        }

        public static void setdead(bool df)
        {
            alldead = df;
        }

        public static void testpause()
        {
            int i;
            if (pausef)
            {
                soundpause();
                sett2val(40);
                setsoundt2();
                cleartopline();
                outtext(ddap, "PRESS ANY KEY", 80, 0, 1);
                getkey(true);
                cleartopline();
                drawscores(ddap);
                for (i = 0; i < diggers; i++)
                    addscore(ddap, i, 0);
                drawlives(ddap);
                if (!synchvid)
                    curtime = gethrt();
                pausef = false;
            }
            else
                soundpauseoff();
        }

        public static void calibrate()
        {
            volume = (int)(getkips() / 291);
            if (volume == 0)
                volume = 1;
        }

        static int read_levf(string levfname)
        {
            FileStream levf = null;
            try
            {
                try
                {
                    levf = File.OpenRead(levfname);
                }
                catch (FileNotFoundException)
                {
                    levfname += ".DLF";
                    try
                    {
                        levf = File.OpenRead(levfname);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                        Log.Write("read_levf: levels file open error:");
                        return (-1);
                    }
                }

                using (var br = new BinaryReader(levf))
                {
                    try
                    {
                        bonusscore = br.ReadInt32();
                    }
                    catch (Exception ex)
                    {
                        Log.Write("read_levf: levels load error 1");
                        Log.Write(ex);
                        return -1;
                    }
                }

                try
                {
                    byte[] buff = new byte[15];
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            levf.Read(buff, 0, 15);
                            leveldat[i, j] = Encoding.ASCII.GetString(buff);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("read_levf: levels load error 2");
                    Log.Write(ex);
                    return -1;
                }
            }
            finally
            {
                if (levf != null)
                    levf.Close();
            }

            return 0;
        }

        private const string BASE_OPTS = "OUH?QM2CKVL:R:P:S:E:G:I:";
        private const string SDL_OPTS = "F";

        public static void parsecmd(string[] args)
        {
            string word;
            int argch;
            int arg, i = 0, j, speedmul;
            bool sf, gs, norepf, hasopt = false;

            gs = norepf = false;

            for (arg = 1; arg < args.Length; arg++)
            {
                word = args[arg];
                if (word[0] == '/' || word[0] == '-')
                {
                    argch = getarg(word[1], BASE_OPTS + SDL_OPTS, ref hasopt);
                    argch = getarg(word[1], BASE_OPTS, ref hasopt);
                    i = 2;
                    if (argch != -1 && hasopt && word[2] == ':')
                    {
                        i = 3;
                    }
                    if (argch == 'L')
                    {
                        j = 0;
                        levfname = word.Substring(3);
                        levfflag = true;
                    }
                    if (argch == 'F')
                    {
                        sdl_enable_fullscreen();
                    }
                    if (argch == 'R')
                        recname(word + i);
                    if (argch == 'P' || argch == 'E')
                    {
                        maininit();
                        openplay(word + i);
                        if (escape)
                            norepf = true;
                    }
                    if (argch == 'E')
                    {
                        finish();
                        if (escape)
                            Environment.Exit(0);
                        Environment.Exit(1);
                    }
                    if (argch == 'O' && !norepf)
                    {
                        arg = 0;
                        continue;
                    }
                    if (argch == 'S')
                    {
                        speedmul = 0;
                        while (word[i] != 0)
                            speedmul = 10 * speedmul + word[i++] - '0';
                        if (speedmul > 0)
                        {
                            ftime = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            ftime = 1;
                        }
                        gs = true;
                    }
                    if (argch == 'I')
                        startlev = int.Parse(word.Substring(i));
                    if (argch == 'U')
                        unlimlives = true;
                    if (argch == '?' || argch == 'H' || argch == -1)
                    {
                        if (argch == -1)
                        {
                            Console.WriteLine("Unknown option \"{0}{1}\"", word[0], word[1]);
                        }
                        finish();
                        Console.WriteLine("DIGGER - Copyright (c) 1983 Windmill software");
                        Console.WriteLine("Restored 1998 by AJ Software");
                        Console.WriteLine("https://github.com/sobomax/digger");
                        Console.WriteLine($"Version: {DIGGER_VERSION}\r\n");
                        Console.WriteLine("Command line syntax:");
                        Console.WriteLine("  DIGGER [[/S:]speed] [[/L:]level file] [/C] [/Q] [/M] ");
                        Console.WriteLine("         [/P:playback file]");
                        Console.WriteLine("         [/E:playback file] [/R:record file] [/O] [/K[A]] ");
                        Console.WriteLine("         [/G[:time]] [/2]");
                        Console.WriteLine("         [/V] [/U] [/I:level] ");
                        Console.WriteLine("         [/F]");
                        Console.WriteLine(Environment.NewLine);
                        Console.WriteLine("/C = Use CGA graphics");
                        Console.WriteLine("/Q = Quiet mode (no sound at all)");
                        Console.WriteLine("/M = No music");
                        Console.WriteLine("/R = Record graphics to file");
                        Console.WriteLine("/P = Playback and restart program");
                        Console.WriteLine("/E = Playback and exit program");
                        Console.WriteLine("/O = Loop to beginning of command line");
                        Console.WriteLine("/K = Redefine keyboard");
                        Console.WriteLine("/G = Gauntlet mode");
                        Console.WriteLine("/2 = Two player simultaneous mode");
                        Console.WriteLine("/F = Full-Screen");
                        Console.WriteLine("/U = Allow unlimited lives");
                        Console.WriteLine("/I = Start on a level other than 1");
                        Environment.Exit(1);
                    }
                    if (argch == 'Q')
                        soundflag = false;
                    if (argch == 'M')
                        musicflag = false;
                    if (argch == '2')
                        diggers = 2;
                    if (argch == 'B' || argch == 'C')
                    {
                        ddap.init = cgainit;
                        ddap.pal = cgapal;
                        ddap.inten = cgainten;
                        ddap.clear = cgaclear;
                        ddap.getpix = cgagetpix;
                        ddap.puti = cgaputi;
                        ddap.geti = cgageti;
                        ddap.putim = cgaputim;
                        ddap.write = cgawrite;
                        ddap.title = cgatitle;
                        ddap.init();
                        ddap.pal(0);
                    }
                    if (argch == 'K')
                    {
                        if (word[2] == 'A' || word[2] == 'a')
                            redefkeyb(ddap, true);
                        else
                            redefkeyb(ddap, false);
                    }
                    if (argch == 'Q')
                        quiet = true;
                    if (argch == 'V')
                        synchvid = true;
                    if (argch == 'G')
                    {
                        gtime = 0;
                        while (word[i] != 0)
                            gtime = 10 * gtime + word[i++] - '0';
                        if (gtime > 3599)
                            gtime = 3599;
                        if (gtime == 0)
                            gtime = 120;
                        gauntlet = true;
                    }
                }
                else
                {
                    i = word.Length;
                    if (i < 1)
                        continue;
                    sf = true;
                    if (!gs)
                    {
                        for (j = 0; j < i; j++)
                        {
                            if (word[j] < '0' || word[j] > '9')
                            {
                                sf = false;
                                break;
                            }
                        }
                    }
                    if (sf)
                    {
                        speedmul = 0;
                        j = 0;
                        while (word[j] != 0)
                            speedmul = 10 * speedmul + word[j++] - '0';
                        gs = true;
                        if (speedmul > 0)
                        {
                            ftime = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            ftime = 1;
                        }
                    }
                    else
                    {
                        levfname = word;
                        levfflag = true;
                    }
                }
            }

            if (levfflag)
            {
                if (read_levf(levfname) != 0)
                {
                    Log.Write("levels load error");
                    levfflag = false;
                }
            }
        }

        private static int getarg(char argch, string allargs, ref bool hasopt)
        {
            char c;

            if (char.IsLetterOrDigit(argch))
            {
                c = char.ToUpper(argch);
            }
            else
            {
                c = argch;
            }

            for (int i = 0; i < allargs.Length; ++i)
            {
                char cp = allargs[i];
                if (c == cp)
                {
                    hasopt = allargs[i + 1] == ':';
                    return (c);
                }
            }
            return (-1);
        }

        public static int randno(int n)
        {
            Random r = new Random((int)DateTime.Now.Ticks);
            int randv = r.Next() * 0x15a4e35 + 1;
            return (int)((randv & 0x7fffffff) % n);
        }

        public static int dx_sound_volume;
        public static bool g_bWindowed, use_640x480_fullscreen, use_async_screen_updates;

        public static void inir()
        {
            bool cgaflag;
            string vbuf;

            for (int i = 0; i < NKEYS; i++)
            {
                string kbuf = string.Format("{0}{1}", keynames[i], (i >= 5 && i < 10) ? '2' : 0);
                vbuf = string.Format("{0}/{1}/{2}/{3}/{4}", keycodes[i][0], keycodes[i][1], keycodes[i][2], keycodes[i][3], keycodes[i][4]);
                vbuf = Ini.GetINIString(INI_KEY_SETTINGS, kbuf, vbuf, ININAME);
                krdf[i] = true;
                int j = 0;
                foreach (string keyCode in vbuf.Split('/'))
                    keycodes[i][j++] = int.Parse(keyCode);
            }
            gtime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "GauntletTime", 120, ININAME);
            if (ftime == 0)
            {
                ftime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "Speed", 80000, ININAME);
            }
            gauntlet = Ini.GetINIBool(INI_GAME_SETTINGS, "GauntletMode", false, ININAME);
            vbuf = Ini.GetINIString(INI_GAME_SETTINGS, "Players", "1", ININAME);
            vbuf = vbuf.ToUpperInvariant();
            if (vbuf[0] == '2' && vbuf[1] == 'S')
            {
                diggers = 2;
                nplayers = 1;
            }
            else
            {
                diggers = 1;
                nplayers = int.Parse(vbuf);
                if (nplayers < 1 || nplayers > 2)
                    nplayers = 1;
            }
            soundflag = Ini.GetINIBool(INI_SOUND_SETTINGS, "SoundOn", true, ININAME);
            musicflag = Ini.GetINIBool(INI_SOUND_SETTINGS, "MusicOn", true, ININAME);
            sound_rate = (ushort)Ini.GetINIInt(INI_SOUND_SETTINGS, "Rate", 22050, ININAME);
            sound_length = (ushort)Ini.GetINIInt(INI_SOUND_SETTINGS, "BufferSize", DEFAULT_BUFFER, ININAME);

            if (!quiet)
            {
                volume = 1;
                setupsound = s1setupsound;
                killsound = s1killsound;
                fillbuffer = s1fillbuffer;
                initint8 = s1initint8;
                restoreint8 = s1restoreint8;
                soundoff = s1soundoff;
                setspkrt2 = s1setspkrt2;
                settimer0 = s1settimer0;
                timer0 = s1timer0;
                settimer2 = s1settimer2;
                timer2 = s1timer2;
                soundinitglob(sound_length, sound_rate);
            }
            dx_sound_volume = Ini.GetINIInt(INI_SOUND_SETTINGS, "SoundVolume", 0, ININAME);
            g_bWindowed = true;
            use_640x480_fullscreen = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "640x480", false, ININAME);
            use_async_screen_updates = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Async", true, ININAME);
            synchvid = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Synch", false, ININAME);
            cgaflag = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "CGA", false, ININAME);
            if (cgaflag)
            {
                ddap.init = cgainit;
                ddap.pal = cgapal;
                ddap.inten = cgainten;
                ddap.clear = cgaclear;
                ddap.getpix = cgagetpix;
                ddap.puti = cgaputi;
                ddap.geti = cgageti;
                ddap.putim = cgaputim;
                ddap.write = cgawrite;
                ddap.title = cgatitle;
                ddap.init();
                ddap.pal(0);
            }
            unlimlives = Ini.GetINIBool(INI_GAME_SETTINGS, "UnlimitedLives", false, ININAME);
            startlev = Ini.GetINIInt(INI_GAME_SETTINGS, "StartLevel", 1, ININAME);
        }
    }
}
