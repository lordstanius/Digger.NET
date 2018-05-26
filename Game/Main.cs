/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;

namespace Digger.Net
{
    public class game_data
    {
        public int level = 1;
        public bool levdone;
    }

    public static partial class DiggerC
    {
        public const int DIR_NONE = -1;
        public const int DIR_RIGHT = 0;
        public const int DIR_UP = 2;
        public const int DIR_LEFT = 4;
        public const int DIR_DOWN = 6;

        public const int TYPES = 5;
        public const int SPRITES = (BONUSES + BAGS + MONSTERS + FIREBALLS + DIGGERS);
        public const int BONUSES = 1;
        public const int BAGS = 7;
        public const int MONSTERS = 6;
        public const int FIREBALLS = DIGGERS;
        public const int DIGGERS = 2;

        public const int MAX_W = 320;
        public const int MAX_H = 200;
        public const int CHR_W = 12;
        public const int CHR_H = 12;

        public const int MAX_TEXT_LEN = MAX_W / CHR_W;

        public const int MWIDTH = 15;
        public const int MHEIGHT = 10;
        public const int MSIZE = MWIDTH * MHEIGHT;

        public const string INI_GAME_SETTINGS = "Game";
        public const string INI_GRAPHICS_SETTINGS = "Graphics";
        public const string INI_SOUND_SETTINGS = "Sound";
        public const string INI_KEY_SETTINGS = "Keys";



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

        public const bool MON_NOBBIN = true;
        public const bool MON_HOBBIN = false;

        /* using lesser buffer size will break ie. alsa on linux, no reason to use
         * lesser size anyways...
         */
        public const int DEFAULT_BUFFER = 2048;
        public const int DEF_SND_DEV = 0;
        public const string ININAME = "digger.ini";

        /// <summary>
        /// Version string:
        /// First word: your initials if you have changed anything.
        /// Second word: platform. 
        /// Third word: compilation date in yyyymmdd format.
        /// </summary>
        public const string DIGGER_VERSION = "MS SDL 20180419";

        /* global variables */
        public static string g_playerName;
        public static int g_CurrentPlayer = 0, g_playerCount = 1, g_Penalty = 0, g_Diggers = 1, g_StartingLevel = 1;
        public static uint g_CurrentTime, g_FrameTime;
        public static bool g_hasUnlimitedLives = false, g_isGauntletMode = false, g_isTimeOut = false, g_isVideoSync = false;
        public static uint g_gameTime = 0;
        public static uint randv;

        public static game_data[] gamedat = { new game_data(), new game_data() };

        public static bool levnotdrawn = false, alldead = false;
        public static bool maininited, started;

        public static SdlGraphics gfx;
        public static Level level;
        public static DrawApi drawApi;
        public static Sprites sprites;
        public static SdlTimer timer;
        public static Scores scores;
        public static Record record;
        public static Input input;
        public static Sound sound;
        public static Monsters monsters;
        public static Bags bags;
        public static Digger digger;

        public static void GlobalInit()
        {
            gfx = new SdlGraphicsVga();
            sound = new Sound();
            input = new Input();
            level = new Level(gamedat);
            sprites = new Sprites(gfx);
            timer = new SdlTimer();
            drawApi = new DrawApi(gfx, sprites);
            digger = new Digger(input, drawApi, sound, sprites, level);
            scores = new Scores(gfx, level, digger);
            record = new Record(scores);
            monsters = new Monsters(level, sprites, sound, drawApi, record, scores);
            bags = new Bags(level, sound, drawApi, monsters, sprites, scores, digger);
        }

        public static void maininit()
        {
            if (maininited)
                return;

            calibrate();
            gfx.Initialize();
            gfx.SetPalette(0);
            input.detectjoy();
            sound.initsound();
            record.recstart();

            maininited = true;
        }

        public static void game()
        {
            bool flashplayer = false;
            if (g_isGauntletMode)
            {
                digger.cgtime = g_gameTime * 1193181;
                g_isTimeOut = false;
            }
            digger.initlives();
            alldead = false;
            gfx.Clear();
            g_CurrentPlayer = 0;
            initlevel();
            g_CurrentPlayer = 1;
            initlevel();
            scores.zeroscores();
            digger.bonusvisible = true;
            if (g_playerCount == 2)
                flashplayer = true;
            g_CurrentPlayer = 0;
            while (getalllives() != 0 && !input.escape && !g_isTimeOut)
            {
                while (!alldead && !input.escape && !g_isTimeOut)
                {
                    drawApi.initmbspr();

                    if (record.playing)
                        randv = record.playgetrand();
                    else
                        randv = 0;

                    record.RecordPutRandom(randv);
                    if (levnotdrawn)
                    {
                        levnotdrawn = false;
                        drawscreen(gfx);
                        if (flashplayer)
                        {
                            flashplayer = false;
                            g_playerName = "PLAYER " + (g_CurrentPlayer == 0 ? "1" : "2");
                            cleartopline();
                            for (int j = 0; j < 15; j++)
                            {
                                for (int c = 1; c <= 3; c++)
                                {
                                    drawApi.TextOut(g_playerName, 108, 0, c);
                                    scores.writecurscore(c);
                                    newframe();
                                    if (input.escape)
                                        return;
                                }
                            }
                            scores.drawscores();
                            for (int i = 0; i < g_Diggers; i++)
                                scores.addscore(i, 0);
                        }
                    }
                    else
                        initchars();

                    drawApi.EraseText(8, 108, 0, 3);
                    scores.initscores();
                    digger.drawlives();
                    sound.music(1);

                    input.flushkeybuf();
                    for (int i = 0; i < g_Diggers; i++)
                        input.readdirect(i);

                    while (!alldead && !gamedat[g_CurrentPlayer].levdone && !input.escape && !g_isTimeOut)
                    {
                        g_Penalty = 0;
                        digger.dodigger(bags, monsters, scores);
                        monsters.domonsters(bags, digger);
                        bags.DoBags();
                        if (g_Penalty > 8)
                            monsters.incmont(g_Penalty - 8);
                        testpause();
                        checklevdone();
                    }
                    digger.erasediggers();
                    sound.musicoff();
                    int t = 20;
                    while ((bags.GetNotMovingBags() != 0 || t != 0) && !input.escape && !g_isTimeOut)
                    {
                        if (t != 0)
                            t--;
                        g_Penalty = 0;
                        bags.DoBags();
                        digger.dodigger(bags, monsters, scores);
                        monsters.domonsters(bags, digger);
                        if (g_Penalty < 8)
                            t = 0;
                    }
                    sound.soundstop();
                    for (int i = 0; i < g_Diggers; i++)
                        digger.killfire(i);

                    digger.erasebonus();
                    bags.Cleanup();
                    drawApi.SaveField();
                    monsters.EraseMonsters();
                    record.recputeol();
                    if (record.playing)
                        record.playskipeol();
                    if (input.escape)
                        record.recputeog();
                    if (gamedat[g_CurrentPlayer].levdone)
                        sound.soundlevdone(input);
                    if (countem() == 0 || gamedat[g_CurrentPlayer].levdone)
                    {
                        for (int i = g_CurrentPlayer; i < g_Diggers + g_CurrentPlayer; i++)
                            if (digger.getlives(i) > 0 && !digger.digalive(i))
                                digger.declife(i);
                        digger.drawlives();
                        gamedat[g_CurrentPlayer].level++;
                        if (gamedat[g_CurrentPlayer].level > 1000)
                            gamedat[g_CurrentPlayer].level = 1000;
                        initlevel();
                    }
                    else if (alldead)
                    {
                        for (int i = g_CurrentPlayer; i < g_CurrentPlayer + g_Diggers; i++)
                            if (digger.getlives(i) > 0)
                                digger.declife(i);
                        digger.drawlives();
                    }
                    if ((alldead && getalllives() == 0 && !g_isGauntletMode && !input.escape) || g_isTimeOut)
                        scores.endofgame();
                }
                alldead = false;
                if (g_playerCount == 2 && digger.getlives(1 - g_CurrentPlayer) != 0)
                {
                    g_CurrentPlayer = 1 - g_CurrentPlayer;
                    flashplayer = levnotdrawn = true;
                }
            }
        }

        public static int mainprog()
        {
            int frame, t;
            monster_obj nobbin, hobbin;
            digger_obj odigger = null;
            obj_position newpos;
            scores.loadscores();
            input.escape = false;
            nobbin = null;
            hobbin = null;
            do
            {
                sound.soundstop();
                drawApi.creatembspr();
                input.detectjoy();
                gfx.Clear();
                gfx.DrawTitleScreen();
                drawApi.TextOut("D I G G E R", 100, 0, 3);
                shownplayers();
                scores.showtable(gfx);
                started = false;
                frame = 0;
                newframe();
                input.teststart();
                while (!started)
                {
                    started = input.teststart();
                    if (input.mode_change)
                    {
                        switchnplayers();
                        shownplayers();
                        input.mode_change = false;
                    }
                    if (frame == 0)
                        for (t = 54; t < 174; t += 12)
                            drawApi.EraseText(12, 164, t, 0);
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
                        drawApi.TextOut("NOBBIN", 216, 64, 2);
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
                        drawApi.TextOut("HOBBIN", 216, 83, 2);
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
                        drawApi.TextOut("DIGGER", 216, 102, 2);
                    if (frame == 178)
                    {
                        sprites.movedrawspr(FIRSTBAG, 184, 120);
                        drawApi.DrawGold(0, 0, 184, 120);
                    }
                    if (frame == 183)
                        drawApi.TextOut("GOLD", 216, 121, 2);
                    if (frame == 198)
                        drawApi.DrawEmerald(184, 141);
                    if (frame == 203)
                        drawApi.TextOut("EMERALD", 216, 140, 2);
                    if (frame == 218)
                        drawApi.drawbonus(184, 158);
                    if (frame == 223)
                        drawApi.TextOut("BONUS", 216, 159, 2);
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
                if (record.savedrf)
                {
                    if (record.gotname)
                    {
                        record.recsavedrf();
                        record.gotgame = false;
                    }
                    record.savedrf = false;
                    continue;
                }
                if (input.escape)
                    break;
                record.recinit();
                game();
                record.gotgame = true;
                if (record.gotname)
                {
                    record.recsavedrf();
                    record.gotgame = false;
                }
                record.savedrf = false;
                input.escape = false;
            } while (!input.escape);
            return 0;
        }

        public static void newframe()
        {
            if (g_isVideoSync)
            {
                for (; g_CurrentTime < g_FrameTime; g_CurrentTime += 17094)
                { /* 17094 = ticks in a refresh */
                    input.checkkeyb(sound);
                }
                g_CurrentTime -= g_FrameTime;
            }
            else
            {
                timer.SyncFrame();
                input.checkkeyb(sound);
                gfx.UpdateScreen();
            }
        }

        static bool quiet = false;
        static ushort sound_rate, sound_length;

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
                if (possible_modes[i].gauntlet != g_isGauntletMode)
                    continue;
                if (possible_modes[i].nplayers != g_playerCount)
                    continue;
                if (possible_modes[i].diggers != g_Diggers)
                    continue;
                break;
            }
            return i;
        }

        public static void shownplayers()
        {
            game_mode gmp;

            drawApi.EraseText(10, 180, 25, 3);
            drawApi.EraseText(12, 170, 39, 3);
            gmp = possible_modes[getnmode()];
            drawApi.TextOut(gmp.title[0].text, gmp.title[0].xpos, 25, 3);
            drawApi.TextOut(gmp.title[1].text, gmp.title[1].xpos, 39, 3);
        }

        public static int getalllives()
        {
            int t = 0, i;
            for (i = g_CurrentPlayer; i < g_Diggers + g_CurrentPlayer; i++)
                t += digger.getlives(i);
            return t;
        }

        public static void switchnplayers()
        {
            int i = getnmode();
            int j = possible_modes[i].last ? 0 : i + 1;
            g_isGauntletMode = possible_modes[j].gauntlet;
            g_playerCount = possible_modes[j].nplayers;
            g_Diggers = possible_modes[j].diggers;
        }

        public static void initlevel()
        {
            gamedat[g_CurrentPlayer].levdone = false;
            drawApi.MakeField(level);
            makeemfield();
            bags.Initialize();
            levnotdrawn = true;
        }

        public static void drawscreen(SdlGraphics ddap)
        {
            drawApi.creatembspr();
            drawApi.DrawStatistics(ddap, level);
            bags.DrawBags();
            drawemeralds();
            digger.initdigger();
            monsters.Initialize();
        }

        public static void initchars()
        {
            drawApi.initmbspr();
            digger.initdigger();
            monsters.Initialize();
        }

        public static void checklevdone()
        {
            if ((countem() == 0 || monsters.monleft() == 0) && digger.isalive())
                gamedat[g_CurrentPlayer].levdone = true;
            else
                gamedat[g_CurrentPlayer].levdone = false;
        }

        public static void incpenalty()
        {
            g_Penalty++;
        }

        public static void cleartopline()
        {
            drawApi.EraseText(26, 0, 0, 3);
            drawApi.EraseText(1, 308, 0, 3);
        }

        public static void setdead(bool df)
        {
            alldead = df;
        }

        public static void testpause()
        {
            int i;
            if (input.pausef)
            {
                sound.soundpause();
                sound.sett2val(40);
                sound.setsoundt2();
                cleartopline();
                drawApi.TextOut("PRESS ANY KEY", 80, 0, 1);
                input.keyboard.GetKey(true);
                cleartopline();
                scores.drawscores();
                for (i = 0; i < g_Diggers; i++)
                    scores.addscore(i, 0);
                digger.drawlives();
                if (!g_isVideoSync)
                {
                    timer.SyncFrame();
                    gfx.UpdateScreen();
                }
                input.pausef = false;
            }
            else
                sound.soundpauseoff();
        }

        public static void calibrate()
        {
            sound.volume = 1;
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
                        level.levfname = word.Substring(3);
                        level.levfflag = true;
                    }
                    if (argch == 'F')
                    {
                        gfx.EnableFullScreen();
                    }
                    if (argch == 'R')
                        record.recname(word + i);
                    if (argch == 'P' || argch == 'E')
                    {
                        maininit();
                        record.openplay(word + i);
                        if (input.escape)
                            norepf = true;
                    }
                    if (argch == 'E')
                    {
                        if (input.escape)
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
                            g_FrameTime = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            g_FrameTime = 1;
                        }
                        gs = true;
                    }
                    if (argch == 'I')
                        g_StartingLevel = int.Parse(word.Substring(i));
                    if (argch == 'U')
                        g_hasUnlimitedLives = true;
                    if (argch == '?' || argch == 'H' || argch == -1)
                    {
                        if (argch == -1)
                            Console.WriteLine("Unknown option \"{0}{1}\"", word[0], word[1]);

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
                        sound.soundflag = false;
                    if (argch == 'M')
                        sound.musicflag = false;
                    if (argch == '2')
                        g_Diggers = 2;
                    if (argch == 'B' || argch == 'C')
                    {
                        gfx = new SdlGraphicsCga();
                    }
                    if (argch == 'K')
                    {
                        if (word[2] == 'A' || word[2] == 'a')
                            Keyboard.Redefine(input, drawApi, true);
                        else
                            Keyboard.Redefine(input, drawApi, false);
                    }
                    if (argch == 'Q')
                        quiet = true;
                    if (argch == 'V')
                        g_isVideoSync = true;
                    if (argch == 'G')
                    {
                        g_gameTime = 0;
                        while (word[i] != 0)
                            g_gameTime = 10 * g_gameTime + word[i++] - '0';
                        if (g_gameTime > 3599)
                            g_gameTime = 3599;
                        if (g_gameTime == 0)
                            g_gameTime = 120;
                        g_isGauntletMode = true;
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
                            g_FrameTime = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            g_FrameTime = 1;
                        }
                    }
                    else
                    {
                        level.levfname = word;
                        level.levfflag = true;
                    }
                }
            }

            if (level.levfflag)
            {
                if (level.read_levf() != 0)
                {
                    DebugLog.Write("levels load error");
                    level.levfflag = false;
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

        public static bool g_bWindowed, use_640x480_fullscreen, use_async_screen_updates;

        public static void inir()
        {
            bool cgaflag;
            string vbuf;

            for (int i = 0; i < Input.NKEYS; i++)
            {
                string kbuf = string.Format("{0}{1}", Keyboard.KeyNames[i], (i >= 5 && i < 10) ? '2' : 0);
                vbuf = string.Format("{0}/{1}/{2}/{3}/{4}",
                    input.keyboard.keycodes[i][0],
                    input.keyboard.keycodes[i][1],
                    input.keyboard.keycodes[i][2],
                    input.keyboard.keycodes[i][3],
                    input.keyboard.keycodes[i][4]);
                vbuf = Ini.GetINIString(INI_KEY_SETTINGS, kbuf, vbuf, ININAME);
                input.krdf[i] = true;
                int j = 0;
                foreach (string keyCode in vbuf.Split('/'))
                    input.keyboard.keycodes[i][j++] = int.Parse(keyCode);
            }
            g_gameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "GauntletTime", 120, ININAME);
            if (g_FrameTime == 0)
            {
                g_FrameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "Speed", 80000, ININAME);
            }
            g_isGauntletMode = Ini.GetINIBool(INI_GAME_SETTINGS, "GauntletMode", false, ININAME);
            vbuf = Ini.GetINIString(INI_GAME_SETTINGS, "Players", "1", ININAME);
            vbuf = vbuf.ToUpperInvariant();
            if (vbuf[0] == '2' && vbuf[1] == 'S')
            {
                g_Diggers = 2;
                g_playerCount = 1;
            }
            else
            {
                g_Diggers = 1;
                g_playerCount = int.Parse(vbuf);
                if (g_playerCount < 1 || g_playerCount > 2)
                    g_playerCount = 1;
            }
            sound.soundflag = Ini.GetINIBool(INI_SOUND_SETTINGS, "SoundOn", true, ININAME);
            sound.musicflag = Ini.GetINIBool(INI_SOUND_SETTINGS, "MusicOn", true, ININAME);
            sound_rate = (ushort)Ini.GetINIInt(INI_SOUND_SETTINGS, "Rate", 22050, ININAME);
            sound_length = (ushort)Ini.GetINIInt(INI_SOUND_SETTINGS, "BufferSize", DEFAULT_BUFFER, ININAME);

            if (!quiet)
            {
                sound.volume = 1;
                sound.soundinitglob(sound_length, sound_rate);
            }

            g_bWindowed = true;
            use_640x480_fullscreen = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "640x480", false, ININAME);
            use_async_screen_updates = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Async", true, ININAME);
            g_isVideoSync = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Synch", false, ININAME);
            cgaflag = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "CGA", false, ININAME);
            if (cgaflag)
                gfx = new SdlGraphicsCga();

            g_hasUnlimitedLives = Ini.GetINIBool(INI_GAME_SETTINGS, "UnlimitedLives", false, ININAME);
            g_StartingLevel = Ini.GetINIInt(INI_GAME_SETTINGS, "StartLevel", 1, ININAME);
        }

        public static int emmask = 0;

        public static void drawemeralds()
        {
            emmask = (short)(1 << DiggerC.g_CurrentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emfield[y * MWIDTH + x] & emmask) != 0)
                        drawApi.DrawEmerald(x * 20 + 12, y * 18 + 21);
        }

        public static void makeemfield()
        {
            emmask = (short)(1 << DiggerC.g_CurrentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (level.getlevch(x, y, level.levplan()) == 'C')
                        emfield[y * MWIDTH + x] |= (byte)emmask;
                    else
                        emfield[y * MWIDTH + x] &= (byte)~emmask;
        }

        static short[] embox = { 8, 12, 12, 9, 16, 12, 6, 9 };
        public static byte[] emfield = new byte[MSIZE];

        public static bool hitemerald(int x, int y, int rx, int ry, int dir)
        {
            bool hit = false;
            int r;
            if (dir != DIR_RIGHT && dir != DIR_UP && dir != DIR_LEFT && dir != DIR_DOWN)
                return hit;
            if (dir == DIR_RIGHT && rx != 0)
                x++;
            if (dir == DIR_DOWN && ry != 0)
                y++;
            if (dir == DIR_RIGHT || dir == DIR_LEFT)
                r = rx;
            else
                r = ry;
            if ((emfield[y * MWIDTH + x] & emmask) != 0)
            {
                if (r == embox[dir])
                {
                    drawApi.DrawEmerald(x * 20 + 12, y * 18 + 21);
                    DiggerC.incpenalty();
                }
                if (r == embox[dir + 1])
                {
                    drawApi.EraseEmerald(x * 20 + 12, y * 18 + 21);
                    DiggerC.incpenalty();
                    hit = true;
                    emfield[y * MWIDTH + x] &= (byte)~emmask;
                }
            }
            return hit;
        }

        public static int countem()
        {
            int n = 0;
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emfield[y * MWIDTH + x] & emmask) != 0)
                        n++;
            return n;
        }

        public static void killemerald(int x, int y)
        {
            if ((emfield[(y + 1) * MWIDTH + x] & emmask) != 0)
            {
                emfield[(y + 1) * MWIDTH + x] &= (byte)~emmask;
                drawApi.EraseEmerald(x * 20 + 12, (y + 1) * 18 + 21);
            }
        }
    }
}
