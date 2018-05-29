/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System;

namespace Digger.Net
{
    public class GameData
    {
        public int level = 1;
        public bool IsLevelDone;
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
        private const string INI_FILE_NAME = "digger.ini";

        private const int SOUND_BUFFER_SIZE = 4096;
        private const int SOUND_RATE = 44100;
        private const int DEF_SND_DEV = 0;

        private const string BASE_OPTS = "OUH?QM2CKL:R:P:S:E:G:I:";
        private const string SDL_OPTS = "F";
        private const string DIGGER_VERSION = Const.DIGGER_VERSION;

        private struct Label
        {
            public string text;
            public int xpos;

            public Label(string text, int xpos)
            {
                this.text = text;
                this.xpos = xpos;
            }
        };

        private struct GameMode
        {
            public bool gauntlet;
            public int nplayers;
            public int diggers;
            public Label[] title;

            public GameMode(bool gauntlet, int nplayers, int diggers, Label[] title)
            {
                this.gauntlet = gauntlet;
                this.nplayers = nplayers;
                this.diggers = diggers;
                this.title = title;
            }
        }

        private readonly GameMode[] possible_modes = {
            new GameMode(false, 1, 1, new Label[]{ new Label("ONE", 220), new Label(" PLAYER ", 192)}),
            new GameMode(false, 2, 1, new Label[]{ new Label("TWO", 220), new Label(" PLAYERS", 184)}),
            new GameMode(false, 1, 2, new Label[]{ new Label("TWO PLAYER", 180), new Label( "SIMULTANEOUS", 170)}),
            new GameMode(true, 1, 1, new Label[]{ new Label("GAUNTLET", 192), new Label( "MODE", 216)}),
            new GameMode(true, 1, 2, new Label[]{ new Label("TWO PLAYER", 180), new Label( "GAUNTLET", 192)})
        };

        private GameData[] gamedat = { new GameData(), new GameData() };

        public string playerName;
        public int currentPlayer;
        public int playerCount = 1;
        public int diggerCount = 1;
        public int startingLevel = 1;
        public uint currentTime;
        public bool hasUnlimitedLives;
        public bool isGauntletMode;
        public bool isTimeOut;
        public uint gameTime;

        public bool levelNotDrawn;
        public bool isEverybodyDead;
        public bool isInitialized;
        public bool isStarted;
        public bool isGameCycleEnded;
        public bool isVideoModeChanged;
        public bool shouldExit;

        public Level level;
        public Video video;
        public Sprites sprites;
        public SDL_Timer timer;
        public Scores scores;
        public Recording record;
        public Input input;
        public Sound sound;
        public Monsters monsters;
        public Bags bags;
        public Diggers diggers;
        public Emeralds emeralds;

        private bool quiet = false;
        private int penalty = 0;

        public Game()
        {
            timer = new SDL_Timer();
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

        public int LevelNo => gamedat[currentPlayer].level;

        public void LoadSettings()
        {
            bool cgaflag;
            string vbuf;

            for (int i = 0; i < input.KeyCount; i++)
            {
                string kbuf = GetKeyboardBufferKey(i);
                vbuf = GetKeyboardBufferValue(i);
                vbuf = Ini.GetINIString(INI_KEY_SETTINGS, kbuf, null, INI_FILE_NAME);
                if (vbuf != null)
                {
                    int j = 0;
                    foreach (string keyCode in vbuf.Split(','))
                        input.KeyCodes[i][j++] = int.Parse(keyCode);
                }
            }
            gameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "GauntletTime", Const.DEFAULT_GAUNTLET_TIME, INI_FILE_NAME);
            timer.FrameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "Speed", Const.DEFAULT_FRAME_TIME, INI_FILE_NAME);
            isGauntletMode = Ini.GetINIBool(INI_GAME_SETTINGS, "GauntletMode", false, INI_FILE_NAME);
            vbuf = Ini.GetINIString(INI_GAME_SETTINGS, "Players", "1", INI_FILE_NAME);
            vbuf = vbuf.ToUpperInvariant();
            if (vbuf[0] == '2' && (vbuf.Length > 1 && vbuf[1] == 'S'))
            {
                diggerCount = 2;
                playerCount = 1;
            }
            else
            {
                diggerCount = 1;
                playerCount = int.Parse(vbuf);
                if (playerCount < 1 || playerCount > 2)
                    playerCount = 1;
            }
            sound.isSoundEnabled = Ini.GetINIBool(INI_SOUND_SETTINGS, "SoundOn", true, INI_FILE_NAME);
            sound.isMusicEnabled = Ini.GetINIBool(INI_SOUND_SETTINGS, "MusicOn", true, INI_FILE_NAME);

            if (!quiet)
            {
                sound.volume = 1;
                sound.SoundInitGlobal(SOUND_BUFFER_SIZE, SOUND_RATE);
            }

            cgaflag = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "CGA", false, INI_FILE_NAME);
            if (cgaflag)
                video.SetVideoMode(VideoMode.CGA);

            video.isFullScreen = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Fullscreen", false, INI_FILE_NAME);
            hasUnlimitedLives = Ini.GetINIBool(INI_GAME_SETTINGS, "UnlimitedLives", false, INI_FILE_NAME);
            startingLevel = Ini.GetINIInt(INI_GAME_SETTINGS, "StartLevel", 1, INI_FILE_NAME);
        }

        public void SaveSettings()
        {
            System.IO.File.Delete(INI_FILE_NAME);

            if (gameTime != Const.DEFAULT_GAUNTLET_TIME)
                Ini.WriteINIInt(INI_GAME_SETTINGS, "GauntletTime", (int)gameTime, INI_FILE_NAME);

            if (timer.FrameTime != Const.DEFAULT_FRAME_TIME)
                Ini.WriteINIInt(INI_GAME_SETTINGS, "Speed", (int)timer.FrameTime, INI_FILE_NAME);

            if (isGauntletMode)
                Ini.WriteINIBool(INI_GAME_SETTINGS, "GauntletMode", isGauntletMode, INI_FILE_NAME);

            if (playerCount > 1)
                Ini.WriteINIString(INI_GAME_SETTINGS, "Players", string.Format("{0}{1}", playerCount, diggerCount == 2 ? "S" : null), INI_FILE_NAME);

            if (!sound.isSoundEnabled)
                Ini.WriteINIBool(INI_SOUND_SETTINGS, "SoundOn", sound.isSoundEnabled, INI_FILE_NAME);

            if (!sound.isMusicEnabled)
                Ini.WriteINIBool(INI_SOUND_SETTINGS, "MusicOn", sound.isMusicEnabled, INI_FILE_NAME);

            if (video.isFullScreen)
                Ini.WriteINIBool(INI_GRAPHICS_SETTINGS, "Fullscreen", video.isFullScreen, INI_FILE_NAME);

            if (video.VideoMode == VideoMode.CGA)
                Ini.WriteINIBool(INI_GRAPHICS_SETTINGS, "CGA", true, INI_FILE_NAME);

            if (hasUnlimitedLives)
                Ini.GetINIBool(INI_GAME_SETTINGS, "UnlimitedLives", hasUnlimitedLives, INI_FILE_NAME);

            if (startingLevel != 1)
                Ini.GetINIInt(INI_GAME_SETTINGS, "StartLevel", startingLevel, INI_FILE_NAME);

            WriteIniKeySettings();
        }

        private string GetKeyboardBufferValue(int i)
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}",
                input.KeyCodes[i][0],
                input.KeyCodes[i][1],
                input.KeyCodes[i][2],
                input.KeyCodes[i][3],
                input.KeyCodes[i][4]);
        }

        private string GetKeyboardBufferKey(int i)
        {
            return string.Format("{0}{1}", Keys.KeyNames[i], (i >= 5 && i < 10) ? " (Player 2)" : null);
        }

        public void ParseCmdLine(string[] args)
        {
            bool sf, gs = false, norepf = false, hasopt = false;

            for (int arg = 0; arg < args.Length; arg++)
            {
                string word = args[arg];
                if (word[0] == '/' || word[0] == '-')
                {
                    int argch = GetArgument(word[1], BASE_OPTS + SDL_OPTS, ref hasopt);
                    int i = 2;
                    if (argch != -1 && hasopt && word[2] == ':')
                        i = 3;

                    if (argch == 'L')
                    {
                        level.LevelFileName = word.Substring(3);
                        level.IsUsingLevelFile = true;
                    }

                    if (argch == 'F')
                        video.isFullScreen = true;

                    if (argch == 'R')
                        record.SetRecordName(word.Substring(i));

                    if (argch == 'P' || argch == 'E')
                    {
                        Initialize();
                        try
                        {
                            record.OpenPlay(word.Substring(i));
                            if (isGameCycleEnded)
                                norepf = true;
                        }
                        catch (Exception ex)
                        {
                            isGameCycleEnded = true;
                            Log.Write("Error reading record file: " + ex);
                        }
                    }

                    if (argch == 'E')
                    {
                        isGameCycleEnded = true;
                        return;
                    }

                    if (argch == 'O' && !norepf)
                    {
                        arg = 0;
                        continue;
                    }
                    if (argch == 'S')
                    {
                        int speedmul = 0;
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
                        startingLevel = int.Parse(word.Substring(i));

                    if (argch == 'U')
                        hasUnlimitedLives = true;

                    if (argch == '?' || argch == 'H' || argch == -1)
                    {
                        if (argch == -1)
                            Console.WriteLine("Unknown option \"{0}{1}\"", word[0], word[1]);

                        PrintUsage();
                        shouldExit = true;
                        return;
                    }

                    if (argch == 'Q')
                        sound.isSoundEnabled = false;

                    if (argch == 'M')
                        sound.isMusicEnabled = false;

                    if (argch == '2')
                        diggerCount = 2;

                    if (argch == 'B' || argch == 'C')
                        video.SetVideoMode(VideoMode.CGA);

                    if (argch == 'K')
                    {
                        if (word.Length > 2 && char.ToUpper(word[2]) == 'A')
                            Keys.Redefine(this, true);
                        else
                            Keys.Redefine(this, false);
                    }
                    if (argch == 'Q')
                        quiet = true;
                    if (argch == 'G')
                    {
                        gameTime = 0;
                        while (word[i] != 0)
                            gameTime = 10 * gameTime + word[i++] - '0';

                        if (gameTime > 3599)
                            gameTime = 3599;

                        if (gameTime == 0)
                            gameTime = 120;

                        isGauntletMode = true;
                    }
                }
                else
                {
                    int i = word.Length;
                    if (i < 1)
                        continue;
                    sf = true;
                    if (!gs)
                    {
                        for (int j = 0; j < i; j++)
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
                        int speedmul = 0;
                        int j = 0;
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
                    Log.Write(ex);
                    level.IsUsingLevelFile = false;
                }
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine("DIGGER - Copyright (c) 1983 Windmill software");
            Console.WriteLine("Restored 1998 by AJ Software");
            Console.WriteLine("https://github.com/lordstanius/digger");
            Console.WriteLine($"Version: {DIGGER_VERSION}\r\n");
            Console.WriteLine("Command line syntax:");
            Console.WriteLine("  DIGGER [[/S:]speed] [[/L:]level file] [/C] [/Q] [/M] ");
            Console.WriteLine("         [/P:playback file]");
            Console.WriteLine("         [/E:playback file] [/R:record file] [/O] [/K[A]] ");
            Console.WriteLine("         [/G[:time]] [/2]");
            Console.WriteLine("         [/U] [/I:level] ");
            Console.WriteLine("         [/F]");
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("/C = Use CGA graphics");
            Console.WriteLine("/Q = Quiet mode (no sound at all)");
            Console.WriteLine("/M = No music");
            Console.WriteLine("/R = Record graphics to file");
            Console.WriteLine("/P = Playback and restart program");
            Console.WriteLine("/E = Playback and exit program");
            Console.WriteLine("/O = Loop to beginning of command line");
            Console.WriteLine("/K = Redefine keyboard (A = all keys) ");
            Console.WriteLine("/G = Gauntlet mode");
            Console.WriteLine("/2 = Two player simultaneous mode");
            Console.WriteLine("/F = Full-Screen");
            Console.WriteLine("/U = Allow unlimited lives");
            Console.WriteLine("/I = Start on a level other than 1");
        }

        public void Initialize()
        {
            if (isInitialized)
                return;

            Calibrate();
            video.Initialize();
            input.DetectJoystick();
            sound.Initialize();

            isInitialized = true;
        }

        public void Play()
        {
            if (isGameCycleEnded)
                return;

            int frame, t;
            Digger odigger = null;
            Position newpos;
            Monster nobbin = null;
            Monster hobbin = null;

            scores.LoadScores();
            isGameCycleEnded = false;

            do
            {
                sound.StopSound();
                video.CreateMonsterBagSprites();
                input.DetectJoystick();
                video.Clear();
                video.DrawTitleScreen();
                video.TextOut("D I G G E R", 100, 0, 3);
                ShownPlayers();
                scores.ShowTable();
                isStarted = false;
                frame = 0;
                NewFrame();
                input.teststart();
                while (!isStarted && !isGameCycleEnded)
                {
                    isStarted = input.teststart();
                    if (input.mode_change)
                    {
                        SwitchNPlayers();
                        ShownPlayers();
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
                        nobbin.Animate();

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
                            newpos.dir = DIR_RIGHT;

                        hobbin.Position = newpos;
                    }

                    if (frame == 100)
                        hobbin.Mutate();

                    if (frame > 90)
                        hobbin.Animate();

                    if (frame == 123)
                        video.TextOut("HOBBIN", 216, 83, 2);

                    if (frame == 130)
                    {
                        odigger = new Digger(this, 0, DIR_LEFT, 292, 101);
                        odigger.Put();
                    }

                    if (frame > 130 && frame <= 157)
                        odigger.x -= 4;

                    if (frame > 157)
                        odigger.dir = DIR_RIGHT;

                    if (frame >= 130)
                        odigger.Animate();

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
                        nobbin.Damage();

                    if (frame == 239)
                        nobbin.Kill();

                    if (frame == 242)
                        hobbin.Damage();

                    if (frame == 246)
                        hobbin.Kill();

                    NewFrame();
                    if (frame++ > 250)
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
                if (isGameCycleEnded)
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
                isGameCycleEnded = false;
                isVideoModeChanged = false;
            } while (!isGameCycleEnded && !shouldExit);
        }

        public void NewFrame()
        {
            if (isVideoModeChanged)
                return;

            timer.SyncFrame();
            input.CheckKeyBuffer();
            video.UpdateScreen();
        }

        public void Run()
        {
            bool flashplayer = false;
            if (isGauntletMode)
            {
                diggers.cgtime = gameTime * 1193181;
                isTimeOut = false;
            }

            diggers.InitializeLives();
            isEverybodyDead = false;
            video.Clear();
            currentPlayer = 0;
            InitalizeLevel();
            currentPlayer = 1;
            InitalizeLevel();
            scores.ZeroScores();
            diggers.isBonusVisible = true;
            currentPlayer = 0;
            if (playerCount == 2)
                flashplayer = true;

            while (GetAllLives() != 0 && !isGameCycleEnded && !isTimeOut && !isVideoModeChanged)
            {
                while (!isEverybodyDead && !isGameCycleEnded && !isTimeOut && !isVideoModeChanged)
                {
                    video.InitMonsterBagSprites();

                    uint randVal = record.IsPlaying ? record.PlayGetRand() : 0;
                    record.RecordPutRandom(randVal);
                    if (levelNotDrawn)
                    {
                        levelNotDrawn = false;
                        DrawScreen();
                        if (flashplayer)
                        {
                            flashplayer = false;
                            playerName = "PLAYER " + (currentPlayer == 0 ? "1" : "2");
                            ClearTopLine();
                            for (int j = 0; j < 15; j++)
                            {
                                for (int c = 1; c <= 3; c++)
                                {
                                    video.TextOut(playerName, 108, 0, c);
                                    scores.WriteCurrentScore(c);
                                    NewFrame();
                                    if (isGameCycleEnded)
                                        return;
                                }
                            }
                            scores.DrawScores();
                            for (int i = 0; i < diggerCount; i++)
                                scores.AddScore(i, 0);
                        }
                    }
                    else
                        InitializeChars();

                    video.EraseText(8, 108, 0, 3);
                    scores.InitializeScores();
                    diggers.DrawLives();
                    sound.Music(1);

                    input.FlushKeyBuffer();
                    for (int i = 0; i < diggerCount; i++)
                        input.ReadDirect(i);

                    while (!isEverybodyDead && !gamedat[currentPlayer].IsLevelDone && !isGameCycleEnded && !isTimeOut && !isVideoModeChanged)
                    {
                        penalty = 0;
                        diggers.DoDiggers(bags, monsters, scores);
                        monsters.DoMonsters();
                        bags.DoBags();
                        if (penalty > 8)
                            monsters.IncreaseMonstersTime(penalty - 8);

                        TestPause();
                        CheckIsLevelDone();
                    }
                    diggers.EraseDiggers();
                    sound.MusicOff();
                    int t = 20;
                    while ((bags.GetNotMovingBags() != 0 || t != 0) && !isGameCycleEnded && !isTimeOut)
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
                    sound.StopSound();
                    for (int i = 0; i < diggerCount; i++)
                        diggers.KillFire(i);

                    diggers.EraseBonus();
                    bags.Cleanup();
                    video.SaveField();
                    monsters.EraseMonsters();
                    record.PutEndOfLevel();
                    if (record.IsPlaying)
                        record.PlaySkipEOL();
                    if (isGameCycleEnded)
                        record.PutEndOfGame();
                    if (gamedat[currentPlayer].IsLevelDone)
                        sound.SoundLevelDone(input);
                    if (emeralds.Count() == 0 || gamedat[currentPlayer].IsLevelDone)
                    {
                        for (int i = currentPlayer; i < diggerCount + currentPlayer; i++)
                            if (diggers.GetLives(i) > 0 && !diggers.IsDiggerAlive(i))
                                diggers.DecreaseLife(i);
                        diggers.DrawLives();
                        gamedat[currentPlayer].level++;
                        if (gamedat[currentPlayer].level > 1000)
                            gamedat[currentPlayer].level = 1000;
                        InitalizeLevel();
                    }
                    else if (isEverybodyDead)
                    {
                        for (int i = currentPlayer; i < currentPlayer + diggerCount; i++)
                            if (diggers.GetLives(i) > 0)
                                diggers.DecreaseLife(i);
                        diggers.DrawLives();
                    }
                    if ((isEverybodyDead && GetAllLives() == 0 && !isGauntletMode && !isGameCycleEnded) || isTimeOut)
                        scores.EndOfGame();
                }
                isEverybodyDead = false;
                if (playerCount == 2 && diggers.GetLives(1 - currentPlayer) != 0)
                {
                    currentPlayer = 1 - currentPlayer;
                    flashplayer = levelNotDrawn = true;
                }
            }
        }

        private int GetNMode()
        {
            for (int i = 0; i < possible_modes.Length; i++)
            {
                if (possible_modes[i].gauntlet != isGauntletMode)
                    continue;

                if (possible_modes[i].nplayers != playerCount)
                    continue;

                if (possible_modes[i].diggers != diggerCount)
                    continue;

                return i;
            }

            return possible_modes.Length - 1;
        }

        public void ShownPlayers()
        {
            video.EraseText(10, 180, 25, 3);
            video.EraseText(12, 170, 39, 3);
            GameMode gmp = possible_modes[GetNMode()];
            video.TextOut(gmp.title[0].text, gmp.title[0].xpos, 25, 3);
            video.TextOut(gmp.title[1].text, gmp.title[1].xpos, 39, 3);
        }

        public int GetAllLives()
        {
            int t = 0;
            for (int i = currentPlayer; i < diggerCount + currentPlayer; i++)
                t += diggers.GetLives(i);
            return t;
        }

        public void SwitchNPlayers()
        {
            int i = GetNMode();
            int j = i == possible_modes.Length - 1 ? 0 : i + 1;
            isGauntletMode = possible_modes[j].gauntlet;
            playerCount = possible_modes[j].nplayers;
            diggerCount = possible_modes[j].diggers;
        }

        public void InitalizeLevel()
        {
            gamedat[currentPlayer].IsLevelDone = false;
            video.MakeField(level);
            emeralds.MakeEmeraldField();
            bags.Initialize();
            levelNotDrawn = true;
        }

        public void DrawScreen()
        {
            video.CreateMonsterBagSprites();
            video.DrawStatistics(level);
            bags.DrawBags();
            emeralds.DrawEmeralds();
            diggers.InitializeDiggers();
            monsters.Initialize();
        }

        public void InitializeChars()
        {
            video.InitMonsterBagSprites();
            diggers.InitializeDiggers();
            monsters.Initialize();
        }

        public void CheckIsLevelDone()
        {
            bool isLevelDone = (emeralds.Count() == 0 || monsters.MonstersLeftCount() == 0) && diggers.IsAnyAlive();
            gamedat[currentPlayer].IsLevelDone = isLevelDone;
        }

        public void IncreasePenalty()
        {
            ++penalty;
        }

        public void ClearTopLine()
        {
            video.EraseText(26, 0, 0, 3);
            video.EraseText(1, 308, 0, 3);
        }

        public void SetDead(bool df)
        {
            isEverybodyDead = df;
        }

        public void TestPause()
        {
            if (input.pausef)
            {
                sound.SoundPause();
                sound.sett2val(40);
                sound.SetSoundT2();
                ClearTopLine();
                video.TextOut("PRESS ANY KEY", 80, 0, 1);
                input.GetKey(true);
                ClearTopLine();
                scores.DrawScores();
                for (int i = 0; i < diggerCount; i++)
                    scores.AddScore(i, 0);

                diggers.DrawLives();
                timer.SyncFrame();
                video.UpdateScreen();
                input.pausef = false;
            }
            else
                sound.SoundPauseOff();
        }

        public void Calibrate()
        {
            sound.volume = 1;
        }

        private int GetArgument(char argch, string allargs, ref bool hasopt)
        {
            if (char.IsLetterOrDigit(argch))
                return char.ToUpper(argch);

            char c = argch;

            for (int i = 0; i < allargs.Length; ++i)
            {
                char cp = allargs[i];
                if (c == cp)
                {
                    hasopt = allargs[i + 1] == ':';
                    return c;
                }
            }
            return -1;
        }

        public void WriteIniKeySettings()
        {
            for (int i = 0; i < input.KeyCount; i++)
            {
                if (input.IsKeyRemapped(i))
                {
                    string kbuf = GetKeyboardBufferKey(i);
                    string vbuf = GetKeyboardBufferValue(i);
                    Ini.WriteINIString(INI_KEY_SETTINGS, kbuf, vbuf, INI_FILE_NAME);
                }
            }
        }
    }
}
