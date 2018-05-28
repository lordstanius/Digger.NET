/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;

namespace Digger.Net
{
    public class GameData
    {
        public int level = 1;
        public bool levdone;
    }

    public class Game
    {
        private const int DIR_NONE = Const.DIR_NONE;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int MONSTERS = Const.MONSTERS;
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;
        private const int FIRSTBAG = Const.FIRSTBAG;

        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int MSIZE = Const.MSIZE;

        private const string INI_GAME_SETTINGS = "Game";
        private const string INI_GRAPHICS_SETTINGS = "Graphics";
        private const string INI_SOUND_SETTINGS = "Sound";
        private const string INI_KEY_SETTINGS = "Keys";
        private const string ININAME = "digger.ini";

        public const int DEFAULT_BUFFER = 2048;
        public const int DEF_SND_DEV = 0;

        private const string DIGGER_VERSION = Const.DIGGER_VERSION;

        public GameData[] gamedat = { new GameData(), new GameData() };

        public string PlayerName;
        public int CurrentPlayer = 0;
        public int PlayerCount = 1;
        public int DiggerCount = 1;
        public int StartingLevel = 1;
        public uint CurrentTime;
        public bool HasUnlimitedLives = false;
        public bool IsGauntletMode = false;
        public bool IsTimeOut = false;
        public bool IsVideoSync = false;
        public uint gameTime = 0;
        public uint randv;

        public bool levnotdrawn = false, IsEverybodyDead = false;
        public bool IsInitialized, started;

        public Level level;
        public Video video;
        public Sprites sprites;
        public ITimer timer;
        public Scores scores;
        public Recording record;
        public Input input;
        public Sound sound;
        public Monsters monsters;
        public Bags bags;
        public Diggers diggers;
        public Emeralds emeralds;

        private int penalty = 0;

        public Game()
        {
            timer = new SdlTimer();
            sound = new Sound(this);
            input = new Input(this);
            level = new Level(this);
            sprites = new Sprites(this);
            video = new Video(this);
            emeralds = new Emeralds(this);
            diggers = new Diggers(this);
            scores = new Scores(this);
            record = new Recording(this);
            monsters = new Monsters(this);
            bags = new Bags(this);
        }

        public static void Main(string[] args)
        {
            var game = new Game();

            try
            {
                game.ReadIni();
                game.ParseCmdLine(args);
                game.Initialize();
                game.GameLoop();
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            calibrate();
            video.Initialize();
            input.DetectJoystick();
            sound.Initialize();

            IsInitialized = true;
        }

        public void Run()
        {
            bool flashplayer = false;
            if (IsGauntletMode)
            {
                diggers.cgtime = gameTime * 1193181;
                IsTimeOut = false;
            }

            diggers.InitializeLives();
            IsEverybodyDead = false;
            video.Clear();
            CurrentPlayer = 0;
            initlevel();
            CurrentPlayer = 1;
            initlevel();
            scores.zeroscores();
            diggers.bonusvisible = true;
            if (PlayerCount == 2)
                flashplayer = true;
            CurrentPlayer = 0;
            while (GetAllLives() != 0 && !input.IsGameCycleEnded && !IsTimeOut)
            {
                while (!IsEverybodyDead && !input.IsGameCycleEnded && !IsTimeOut)
                {
                    video.InitializeMBSprite();

                    if (record.IsPlaying)
                        randv = record.PlayGetRand();
                    else
                        randv = 0;

                    record.RecordPutRandom(randv);
                    if (levnotdrawn)
                    {
                        levnotdrawn = false;
                        DrawScreen();
                        if (flashplayer)
                        {
                            flashplayer = false;
                            PlayerName = "PLAYER " + (CurrentPlayer == 0 ? "1" : "2");
                            ClearTopLine();
                            for (int j = 0; j < 15; j++)
                            {
                                for (int c = 1; c <= 3; c++)
                                {
                                    video.TextOut(PlayerName, 108, 0, c);
                                    scores.writecurscore(c);
                                    NewFrame();
                                    if (input.IsGameCycleEnded)
                                        return;
                                }
                            }
                            scores.drawscores();
                            for (int i = 0; i < DiggerCount; i++)
                                scores.AddScore(i, 0);
                        }
                    }
                    else
                        initchars();

                    video.EraseText(8, 108, 0, 3);
                    scores.initscores();
                    diggers.DrawLives();
                    sound.music(1);

                    input.flushkeybuf();
                    for (int i = 0; i < DiggerCount; i++)
                        input.readdirect(i);

                    while (!IsEverybodyDead && !gamedat[CurrentPlayer].levdone && !input.IsGameCycleEnded && !IsTimeOut)
                    {
                        penalty = 0;
                        diggers.DoDiggers(bags, monsters, scores);
                        monsters.DoMonsters();
                        bags.DoBags();
                        if (penalty > 8)
                            monsters.IncreaseMonstersTime(penalty - 8);
                        TestPause();
                        checklevdone();
                    }
                    diggers.erasediggers();
                    sound.musicoff();
                    int t = 20;
                    while ((bags.GetNotMovingBags() != 0 || t != 0) && !input.IsGameCycleEnded && !IsTimeOut)
                    {
                        if (t != 0)
                            t--;
                        penalty = 0;
                        bags.DoBags();
                        diggers.DoDiggers(bags, monsters, scores);
                        monsters.DoMonsters();
                        if (penalty < 8)
                            t = 0;
                    }
                    sound.soundstop();
                    for (int i = 0; i < DiggerCount; i++)
                        diggers.killfire(i);

                    diggers.erasebonus();
                    bags.Cleanup();
                    video.SaveField();
                    monsters.EraseMonsters();
                    record.PutEndOfLevel();
                    if (record.IsPlaying)
                        record.PlaySkipEOL();
                    if (input.IsGameCycleEnded)
                        record.PutEndOfGame();
                    if (gamedat[CurrentPlayer].levdone)
                        sound.SoundLevelDone(input);
                    if (emeralds.Count() == 0 || gamedat[CurrentPlayer].levdone)
                    {
                        for (int i = CurrentPlayer; i < DiggerCount + CurrentPlayer; i++)
                            if (diggers.GetLives(i) > 0 && !diggers.IsDiggerAlive(i))
                                diggers.DecreaseLife(i);
                        diggers.DrawLives();
                        gamedat[CurrentPlayer].level++;
                        if (gamedat[CurrentPlayer].level > 1000)
                            gamedat[CurrentPlayer].level = 1000;
                        initlevel();
                    }
                    else if (IsEverybodyDead)
                    {
                        for (int i = CurrentPlayer; i < CurrentPlayer + DiggerCount; i++)
                            if (diggers.GetLives(i) > 0)
                                diggers.DecreaseLife(i);
                        diggers.DrawLives();
                    }
                    if ((IsEverybodyDead && GetAllLives() == 0 && !IsGauntletMode && !input.IsGameCycleEnded) || IsTimeOut)
                        scores.endofgame();
                }
                IsEverybodyDead = false;
                if (PlayerCount == 2 && diggers.GetLives(1 - CurrentPlayer) != 0)
                {
                    CurrentPlayer = 1 - CurrentPlayer;
                    flashplayer = levnotdrawn = true;
                }
            }
        }

        public int GameLoop()
        {
            int frame, t;
            Monster nobbin, hobbin;
            Digger odigger = null;
            Position newpos;
            scores.loadscores();
            input.IsGameCycleEnded = false;
            nobbin = null;
            hobbin = null;
            do
            {
                sound.soundstop();
                video.CreateMBSprite();
                input.DetectJoystick();
                video.Clear();
                video.DrawTitleScreen();
                video.TextOut("D I G G E R", 100, 0, 3);
                shownplayers();
                scores.ShowTable();
                started = false;
                frame = 0;
                NewFrame();
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
                            video.EraseText(12, 164, t, 0);
                    if (frame == 50)
                    {
                        nobbin = new Monster(this, 0, DIR_LEFT, 292, 63);
                        nobbin.Put();
                    }
                    if (frame > 50 && frame <= 77)
                    {
                        newpos = nobbin.Position;
                        newpos.x -= 4;
                        if (frame == 77)
                            newpos.dir = DIR_RIGHT;
                        
                        nobbin.Position = newpos;
                    }
                    if (frame > 50)
                    {
                        nobbin.Animate();
                    }

                    if (frame == 83)
                        video.TextOut("NOBBIN", 216, 64, 2);
                    if (frame == 90)
                    {
                        hobbin = new Monster(this, 1, DIR_LEFT, 292, 82);
                        hobbin.Put();
                    }
                    if (frame > 90 && frame <= 117)
                    {
                        newpos = hobbin.Position;
                        newpos.x -= 4;
                        if (frame == 117)
                        {
                            newpos.dir = DIR_RIGHT;
                        }
                        hobbin.Position = newpos;
                    }
                    if (frame == 100)
                    {
                        hobbin.Mutate();
                    }
                    if (frame > 90)
                    {
                        hobbin.Animate();
                    }
                    if (frame == 123)
                        video.TextOut("HOBBIN", 216, 83, 2);
                    if (frame == 130)
                    {
                        odigger = new Digger(this, 0, DIR_LEFT, 292, 101);
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
                        video.TextOut("DIGGER", 216, 102, 2);
                    if (frame == 178)
                    {
                        sprites.MoveDrawSprite(FIRSTBAG, 184, 120);
                        video.DrawGold(0, 0, 184, 120);
                    }
                    if (frame == 183)
                        video.TextOut("GOLD", 216, 121, 2);
                    if (frame == 198)
                        video.DrawEmerald(184, 141);
                    if (frame == 203)
                        video.TextOut("EMERALD", 216, 140, 2);
                    if (frame == 218)
                        video.DrawBonus(184, 158);
                    if (frame == 223)
                        video.TextOut("BONUS", 216, 159, 2);
                    if (frame == 235)
                    {
                        nobbin.Damage();
                    }
                    if (frame == 239)
                    {
                        nobbin.Kill();
                    }
                    if (frame == 242)
                    {
                        hobbin.Damage();
                    }
                    if (frame == 246)
                    {
                        hobbin.Kill();
                    }
                    NewFrame();
                    frame++;
                    if (frame > 250)
                        frame = 0;
                }
                if (record.SaveDrf)
                {
                    if (record.GotName)
                    {
                        record.SaveRecordFile();
                        record.GotGame = false;
                    }
                    record.SaveDrf = false;
                    continue;
                }
                if (input.IsGameCycleEnded)
                    break;

                record.StartRecording();

                Run();

                record.GotGame = true;

                if (record.GotName)
                {
                    record.SaveRecordFile();
                    record.GotGame = false;
                }
                record.SaveDrf = false;
                input.IsGameCycleEnded = false;
            } while (!input.IsGameCycleEnded);
            return 0;
        }

        public void NewFrame()
        {
            if (IsVideoSync)
            {
                for (; CurrentTime < timer.FrameTime; CurrentTime += 17094)
                { /* 17094 = ticks in a refresh */
                    input.checkkeyb(sound);
                }
                CurrentTime -= timer.FrameTime;
            }
            else
            {
                timer.SyncFrame();
                input.checkkeyb(sound);
                video.UpdateScreen();
            }
        }

        bool quiet = false;
        ushort sound_rate, sound_length;

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

        public game_mode[] possible_modes = {
            new game_mode(false, 1, 1, false, new label[]{ new label("ONE", 220), new label(" PLAYER ", 192)}),
            new game_mode(false, 2, 1, false, new label[]{ new label("TWO", 220), new label(" PLAYERS", 184)}),
            new game_mode(false, 2, 2, false, new label[]{ new label("TWO PLAYER", 180), new label( "SIMULTANEOUS", 170)}),
            new game_mode(true, 1, 1, false, new label[]{ new label("GAUNTLET", 192), new label( "MODE", 216)}),
            new game_mode(true, 1, 2, true, new label[]{ new label("TWO PLAYER", 180), new label( "GAUNTLET", 192)})
        };

        int getnmode()
        {
            int i;

            for (i = 0; !possible_modes[i].last; i++)
            {
                if (possible_modes[i].gauntlet != IsGauntletMode)
                    continue;
                if (possible_modes[i].nplayers != PlayerCount)
                    continue;
                if (possible_modes[i].diggers != DiggerCount)
                    continue;
                break;
            }
            return i;
        }

        public void shownplayers()
        {
            game_mode gmp;

            video.EraseText(10, 180, 25, 3);
            video.EraseText(12, 170, 39, 3);
            gmp = possible_modes[getnmode()];
            video.TextOut(gmp.title[0].text, gmp.title[0].xpos, 25, 3);
            video.TextOut(gmp.title[1].text, gmp.title[1].xpos, 39, 3);
        }

        public int GetAllLives()
        {
            int t = 0;
            for (int i = CurrentPlayer; i < DiggerCount + CurrentPlayer; i++)
                t += diggers.GetLives(i);
            return t;
        }

        public void switchnplayers()
        {
            int i = getnmode();
            int j = possible_modes[i].last ? 0 : i + 1;
            IsGauntletMode = possible_modes[j].gauntlet;
            PlayerCount = possible_modes[j].nplayers;
            DiggerCount = possible_modes[j].diggers;
        }

        public void initlevel()
        {
            gamedat[CurrentPlayer].levdone = false;
            video.MakeField(level);
            emeralds.MakeEmeraldField();
            bags.Initialize();
            levnotdrawn = true;
        }

        public void DrawScreen()
        {
            video.CreateMBSprite();
            video.DrawStatistics(level);
            bags.DrawBags();
            emeralds.DrawEmeralds();
            diggers.InitializeDiggers();
            monsters.Initialize();
        }

        public void initchars()
        {
            video.InitializeMBSprite();
            diggers.InitializeDiggers();
            monsters.Initialize();
        }

        public void checklevdone()
        {
            if ((emeralds.Count() == 0 || monsters.MonstersLeftCount() == 0) && diggers.IsAlive())
                gamedat[CurrentPlayer].levdone = true;
            else
                gamedat[CurrentPlayer].levdone = false;
        }

        public void IncreasePenalty()
        {
            penalty++;
        }

        public void ClearTopLine()
        {
            video.EraseText(26, 0, 0, 3);
            video.EraseText(1, 308, 0, 3);
        }

        public void SetDead(bool df)
        {
            IsEverybodyDead = df;
        }

        public void TestPause()
        {
            int i;
            if (input.pausef)
            {
                sound.soundpause();
                sound.sett2val(40);
                sound.setsoundt2();
                ClearTopLine();
                video.TextOut("PRESS ANY KEY", 80, 0, 1);
                input.keyboard.GetKey(true);
                ClearTopLine();
                scores.drawscores();
                for (i = 0; i < DiggerCount; i++)
                    scores.AddScore(i, 0);
                diggers.DrawLives();
                if (!IsVideoSync)
                {
                    timer.SyncFrame();
                    video.UpdateScreen();
                }
                input.pausef = false;
            }
            else
                sound.soundpauseoff();
        }

        public void calibrate()
        {
            sound.volume = 1;
        }

        private const string BASE_OPTS = "OUH?QM2CKVL:R:P:S:E:G:I:";
        private const string SDL_OPTS = "F";

        public void ParseCmdLine(string[] args)
        {
            int arg, i = 0, j, speedmul;
            bool sf, gs = false, norepf = false, hasopt = false;

            for (arg = 0; arg < args.Length; arg++)
            {
                string word = args[arg];
                if (word[0] == '/' || word[0] == '-')
                {
                    int argch = GetArgument(word[1], BASE_OPTS + SDL_OPTS, ref hasopt);
                    i = 2;
                    if (argch != -1 && hasopt && word[2] == ':')
                    {
                        i = 3;
                    }
                    if (argch == 'L')
                    {
                        j = 0;
                        level.LevelFileName = word.Substring(3);
                        level.IsUsingLevelFile = true;
                    }
                    if (argch == 'F')
                    {
                        video.EnableFullScreen();
                    }
                    if (argch == 'R')
                        record.SetRecordName(word.Substring(i));
                    if (argch == 'P' || argch == 'E')
                    {
                        Initialize();
                        try
                        {
                            record.OpenPlay(word.Substring(i));
                            if (input.IsGameCycleEnded)
                                norepf = true;
                        }
                        catch (Exception ex)
                        {
                            input.IsGameCycleEnded = true;
                            DebugLog.Write("Error reading record file: " + ex);
                        }
                    }
                    if (argch == 'E')
                    {
                        if (input.IsGameCycleEnded)
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
                            timer.FrameTime = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            timer.FrameTime = 1;
                        }
                        gs = true;
                    }
                    if (argch == 'I')
                        StartingLevel = int.Parse(word.Substring(i));
                    if (argch == 'U')
                        HasUnlimitedLives = true;
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
                        DiggerCount = 2;
                    if (argch == 'B' || argch == 'C')
                    {
                        video.SetVideoMode(VideoMode.CGA);
                    }
                    if (argch == 'K')
                    {
                        if (word[2] == 'A' || word[2] == 'a')
                            Keyboard.Redefine(this, input, video, true);
                        else
                            Keyboard.Redefine(this, input, video, false);
                    }
                    if (argch == 'Q')
                        quiet = true;
                    if (argch == 'V')
                        IsVideoSync = true;
                    if (argch == 'G')
                    {
                        gameTime = 0;
                        while (word[i] != 0)
                            gameTime = 10 * gameTime + word[i++] - '0';
                        if (gameTime > 3599)
                            gameTime = 3599;
                        if (gameTime == 0)
                            gameTime = 120;
                        IsGauntletMode = true;
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
                            timer.FrameTime = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            timer.FrameTime = 1;
                        }
                    }
                    else
                    {
                        level.LevelFileName = word;
                        level.IsUsingLevelFile = true;
                    }
                }
            }

            if (level.IsUsingLevelFile)
            {
                try
                {
                    level.ReadLevelFile();
                }
                catch (Exception ex)
                {
                    DebugLog.Write(ex);
                    level.IsUsingLevelFile = false;
                }
            }
        }

        private int GetArgument(char argch, string allargs, ref bool hasopt)
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

        public bool g_bWindowed, use_640x480_fullscreen, use_async_screen_updates;

        public void ReadIni()
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
            gameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "GauntletTime", 120, ININAME);
            if (timer.FrameTime == 0)
                timer.FrameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "Speed", 80000, ININAME);

            IsGauntletMode = Ini.GetINIBool(INI_GAME_SETTINGS, "GauntletMode", false, ININAME);
            vbuf = Ini.GetINIString(INI_GAME_SETTINGS, "Players", "1", ININAME);
            vbuf = vbuf.ToUpperInvariant();
            if (vbuf[0] == '2' && vbuf[1] == 'S')
            {
                DiggerCount = 2;
                PlayerCount = 1;
            }
            else
            {
                DiggerCount = 1;
                PlayerCount = int.Parse(vbuf);
                if (PlayerCount < 1 || PlayerCount > 2)
                    PlayerCount = 1;
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
            IsVideoSync = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Synch", false, ININAME);
            cgaflag = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "CGA", false, ININAME);
            if (cgaflag)
                video.SetVideoMode(VideoMode.CGA);

            HasUnlimitedLives = Ini.GetINIBool(INI_GAME_SETTINGS, "UnlimitedLives", false, ININAME);
            StartingLevel = Ini.GetINIInt(INI_GAME_SETTINGS, "StartLevel", 1, ININAME);
        }

        public void WriteKeyboardSettings()
        {
            for (int i = 0; i < Input.NKEYS; i++)
            {
                if (input.krdf[i])
                {
                    string kbuf = string.Format("{0}{1}", Keyboard.KeyNames[i], (i >= 5 && i < 10) ? '2' : 0);
                    string vbuf = string.Format("{0}/{1}/{2}/{3}/{4}", input.keyboard.keycodes[i][0], input.keyboard.keycodes[i][1],
                            input.keyboard.keycodes[i][2], input.keyboard.keycodes[i][3], input.keyboard.keycodes[i][4]);
                    Ini.WriteINIString(INI_KEY_SETTINGS, kbuf, vbuf, ININAME);
                }
            }
        }
    }
}
