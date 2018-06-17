/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System;

namespace Digger.Source
{
    public class Game
    {
        private struct GameData
        {
            public int level;
            public bool isLevelDone;
        }

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

        private GameData[] gameData = new GameData[2];

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

        public Drawing drawing;
        public Sprite sprite;
        public SDL_Timer timer;
        public Scores scores;
        public Recorder recorder;
        public Input input;
        public Sound sound;
        public Monsters monsters;
        public Bags bags;
        public Diggers diggers;
        public Emeralds emeralds;
        public Video video;

        private bool quiet = false;
        private int penalty = 0;
        private int randVal;
        private bool flashplayer;
        private VideoMode initialVideoMode = Const.DEFAULT_VIDEO_MODE;

        public Game()
        {
            timer = new SDL_Timer();
            sound = new Sound(this);
            input = new Input(this);
            sprite = new Sprite(this);
            drawing = new Drawing(this);
            emeralds = new Emeralds(this);
            diggers = new Diggers(this);
            scores = new Scores(this);
            recorder = new Recorder(this);
            monsters = new Monsters(this);
            bags = new Bags(this);
            video = new Video();
        }

        public void Init()
        {
            if (isInitialized)
                return;

            for (int i = 0; i < 2; ++i)
                gameData[i].level = startingLevel;

            video.CreateWindow();
            video.Init(initialVideoMode);
            video.SetFullscreenWindow();
            sound.Init();

            isInitialized = true;
        }

        public short RandNo(int n)
        {
            randVal = randVal * 0x15a4e35 + 1;
            return (short)((randVal & 0x7fffffff) % n);
        }

        public int Level => gameData[currentPlayer].level;

        public void LoadSettings()
        {
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
                    {
                        int scanCode = int.Parse(keyCode);
                        if (scanCode != 0)
                            input.KeyCodes[i][j++] = scanCode;
                    }
                }
            }
            gameTime = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "GauntletTime", Const.DEFAULT_GAUNTLET_TIME, INI_FILE_NAME);
            timer.FrameTicks = (uint)Ini.GetINIInt(INI_GAME_SETTINGS, "Speed", Const.DEFAULT_FRAME_TIME, INI_FILE_NAME);
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
                sound.SoundInitGlobal();
            }

            string videoModeStr = Ini.GetINIString(INI_GRAPHICS_SETTINGS, "VideoMode", null, INI_FILE_NAME);
            if (videoModeStr != null)
                initialVideoMode = (VideoMode)Enum.Parse(typeof(VideoMode), videoModeStr);

            video.isFullScreen = Ini.GetINIBool(INI_GRAPHICS_SETTINGS, "Fullscreen", false, INI_FILE_NAME);
            hasUnlimitedLives = Ini.GetINIBool(INI_GAME_SETTINGS, "UnlimitedLives", false, INI_FILE_NAME);
            startingLevel = Ini.GetINIInt(INI_GAME_SETTINGS, "StartLevel", 1, INI_FILE_NAME);
        }

        public void SaveSettings()
        {
            System.IO.File.Delete(INI_FILE_NAME);

            if (gameTime != Const.DEFAULT_GAUNTLET_TIME)
                Ini.WriteINIInt(INI_GAME_SETTINGS, "GauntletTime", (int)gameTime, INI_FILE_NAME);

            if (timer.FrameTicks != Const.DEFAULT_FRAME_TIME)
                Ini.WriteINIInt(INI_GAME_SETTINGS, "Speed", (int)timer.FrameTicks, INI_FILE_NAME);

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

            if (video.VideoMode != Const.DEFAULT_VIDEO_MODE)
                Ini.WriteINIString(INI_GRAPHICS_SETTINGS, "VideoMode", video.VideoMode.ToString(), INI_FILE_NAME);

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
            return string.Format("{0}{1}", Keyboard.KeyNames[i], (i >= 5 && i < 10) ? " (Player 2)" : null);
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
                        Source.Level.LevelFileName = word.Substring(3);
                        Source.Level.IsUsingLevelFile = true;
                    }

                    if (argch == 'F')
                        video.isFullScreen = true;

                    if (argch == 'R')
                        recorder.SetRecordName(word.Substring(i));

                    if (argch == 'P' || argch == 'E')
                    {
                        Init();
                        try
                        {
                            recorder.OpenPlay(word.Substring(i));
                            if (isGameCycleEnded)
                                norepf = true;
                        }
                        catch (Exception ex)
                        {
                            isGameCycleEnded = true;
                            Log.Write(ex);
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
                            timer.FrameTicks = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            timer.FrameTicks = 1;
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
                        isVideoModeChanged = video.SetVideoMode(VideoMode.CGA);

                    if (argch == 'K')
                    {
                        if (word.Length > 2 && char.ToUpper(word[2]) == 'A')
                            Keyboard.Redefine(this, true);
                        else
                            Keyboard.Redefine(this, false);
                    }
                    if (argch == 'Q')
                        quiet = true;

                    if (argch == 'G')
                    {
                        gameTime = 0;
                        for (int j = 0; j < word.Length; ++j)
                            gameTime = 10 * gameTime + word[j++] - '0';

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
                            timer.FrameTicks = (uint)(speedmul * 2000);
                        }
                        else
                        {
                            timer.FrameTicks = 1;
                        }
                    }
                    else
                    {
                        Source.Level.LevelFileName = word;
                        Source.Level.IsUsingLevelFile = true;
                    }
                }
            }

            if (Source.Level.IsUsingLevelFile)
            {
                try
                {
                    Source.Level.ReadLevelFile(ref scores.bonusscore);
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Source.Level.IsUsingLevelFile = false;
                }
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine("DIGGER - Copyright (c) 1983 Windmill software");
            Console.WriteLine("Restored 1998 by AJ Software");
            Console.WriteLine("https://github.com/lordstanius/digger.net");
            Console.WriteLine($"Version: {DIGGER_VERSION}\r\n");
            Console.WriteLine("Command line syntax:");
            Console.WriteLine("  DIGGER [[/S:]speed] [[/L:]level file] [/C] [/Q] [/M] ");
            Console.WriteLine("         [/P:playback file]");
            Console.WriteLine("         [/E:playback file] [/R:recorder file] [/O] [/K[A]] ");
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
            Console.WriteLine("/O = Playback and loop to beginning of command line");
            Console.WriteLine("/K = Redefine keyboard (A = all keys) ");
            Console.WriteLine("/G = Gauntlet mode");
            Console.WriteLine("/2 = Two player simultaneous mode");
            Console.WriteLine("/F = Full-Screen");
            Console.WriteLine("/U = Allow unlimited lives");
            Console.WriteLine("/I = Start on a level other than 1");
        }

        public void Start()
        {
            if (isGameCycleEnded)
                return;

            int frame, t, x = 0;
            scores.LoadScores();
            isGameCycleEnded = false;

            do
            {
                sound.StopSound();
                drawing.CreateMonsterBagSprites();
                video.Clear();
                video.DrawTitleScreen();
                drawing.TextOut("D I G G E R", 100, 0, 3);
                ShownPlayers();
                scores.ShowTable();
                isStarted = false;
                frame = 0;
                NewFrame();
                input.TestStart();
                while (!isStarted && !isGameCycleEnded)
                {
                    isStarted = input.TestStart();
                    if (input.mode_change)
                    {
                        SwitchNPlayers();
                        ShownPlayers();
                        input.mode_change = false;
                    }

                    if (frame == 0)
                        for (t = 54; t < 174; t += 12)
                            drawing.EraseText(12, 164, t, 0);

                    if (frame == 50)
                    {
                        sprite.MoveDrawSprite(FIRSTMONSTER, 292, 63);
                        x = 292;
                    }

                    if (frame > 50 && frame <= 77)
                    {
                        x -= 4;
                        drawing.DrawMonster(0, true, 4, x, 63);
                    }

                    if (frame > 77)
                        drawing.DrawMonster(0, true, 0, 184, 63);

                    if (frame == 83)
                        drawing.TextOut("NOBBIN", 216, 64, 2);

                    if (frame == 90)
                    {
                        sprite.MoveDrawSprite(FIRSTMONSTER + 1, 292, 82);
                        drawing.DrawMonster(1, false, Dir.Left, 292, 82);
                        x = 292;
                    }

                    if (frame > 90 && frame <= 117)
                    {
                        x -= 4;
                        drawing.DrawMonster(1, false, 4, x, 82);
                    }

                    if (frame > 117)
                        drawing.DrawMonster(1, false, Dir.Right, 184, 82);

                    if (frame == 123)
                        drawing.TextOut("HOBBIN", 216, 83, 2);

                    if (frame == 130)
                    {
                        sprite.MoveDrawSprite(FIRSTDIGGER, 292, 101);
                        drawing.DrawDigger(0, Dir.Left, 292, 101, true);
                        x = 292;
                    }

                    if (frame > 130 && frame <= 157)
                    {
                        x -= 4;
                        drawing.DrawDigger(0, 4, x, 101, true);
                    }

                    if (frame > 157)
                        drawing.DrawDigger(0, 0, 184, 101, true);

                    if (frame == 163)
                        drawing.TextOut("DIGGER", 216, 102, 2);

                    if (frame == 178)
                    {
                        sprite.MoveDrawSprite(FIRSTBAG, 184, 120);
                        drawing.DrawGold(0, 0, 184, 120);
                    }

                    if (frame == 183)
                        drawing.TextOut("GOLD", 216, 121, 2);

                    if (frame == 198)
                        drawing.DrawEmerald(184, 141);

                    if (frame == 203)
                        drawing.TextOut("EMERALD", 216, 140, 2);

                    if (frame == 218)
                        drawing.DrawBonus(184, 158);

                    if (frame == 223)
                        drawing.TextOut("BONUS", 216, 159, 2);

                    NewFrame();
                    frame++;
                    if (frame > 250)
                        frame = 0;
                }
                if (isGameCycleEnded)
                    break;

                recorder.StartRecording();

                Run();

                if (recorder.gotName)
                    recorder.SaveRecordFile();
                else if (recorder.saveDrf)
                    recorder.SaveRecordFile();

                recorder.saveDrf = false;
                isGameCycleEnded = false;
                isVideoModeChanged = false;
            } while (!isGameCycleEnded && !shouldExit);
        }

        internal void NewFrame()
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
                    drawing.InitMonsterBagSprites();
                    Play();
                }

                isEverybodyDead = false;
                if (playerCount == 2 && diggers.GetLives(1 - currentPlayer) != 0)
                {
                    currentPlayer = 1 - currentPlayer;
                    flashplayer = levelNotDrawn = true;
                }
            }
        }

        private void Play()
        {
            randVal = recorder.isPlaying ? recorder.PlayGetRand() : (short)Environment.TickCount;
            recorder.RecordPutRandom(randVal);
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
                            drawing.TextOut(playerName, 108, 0, c);
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

            drawing.EraseText(8, 108, 0, 3);
            scores.InitializeScores();
            diggers.DrawLives();
            sound.Music(1);

            input.FlushKeyBuffer();
            for (int i = 0; i < diggerCount; i++)
                input.ReadDirection(i);

            while (!isEverybodyDead && !gameData[currentPlayer].isLevelDone && !isGameCycleEnded && !isTimeOut && !isVideoModeChanged)
            {
                NewFrame();
                penalty = 0;
                diggers.DoDiggers();
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
                diggers.DoDiggers();
                monsters.DoMonsters();
                if (penalty < 8)
                    t = 0;
            }
            sound.StopSound();
            for (int i = 0; i < diggerCount; i++)
                diggers.KillFire(i);

            diggers.EraseBonus();
            bags.Cleanup();
            drawing.SaveField();
            monsters.EraseMonsters();
            recorder.PutEndOfLevel();
            if (recorder.isPlaying)
                recorder.PlaySkipEOL();

            if (isGameCycleEnded)
                recorder.PutEndOfGame();

            if (gameData[currentPlayer].isLevelDone)
                sound.SoundLevelDone(input);

            if (emeralds.Count() == 0 || gameData[currentPlayer].isLevelDone)
            {
                for (int i = currentPlayer; i < diggerCount + currentPlayer; i++)
                    if (diggers.GetLives(i) > 0 && !diggers.IsDiggerAlive(i))
                        diggers.DecreaseLife(i);

                diggers.DrawLives();
                gameData[currentPlayer].level++;
                if (gameData[currentPlayer].level > 1000)
                    gameData[currentPlayer].level = 1000;
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
            drawing.EraseText(10, 180, 25, 3);
            drawing.EraseText(12, 170, 39, 3);
            GameMode gmp = possible_modes[GetNMode()];
            drawing.TextOut(gmp.title[0].text, gmp.title[0].xpos, 25, 3);
            drawing.TextOut(gmp.title[1].text, gmp.title[1].xpos, 39, 3);
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
            gameData[currentPlayer].isLevelDone = false;
            drawing.MakeField();
            emeralds.MakeEmeraldField();
            bags.Init();
            levelNotDrawn = true;
        }

        public void DrawScreen()
        {
            drawing.CreateMonsterBagSprites();
            drawing.DrawStatistics();
            bags.DrawBags();
            emeralds.DrawEmeralds();
            diggers.InitializeDiggers();
            monsters.Init();
        }

        public void InitializeChars()
        {
            drawing.InitMonsterBagSprites();
            diggers.InitializeDiggers();
            monsters.Init();
        }

        public void CheckIsLevelDone()
        {
            bool isLevelDone = (emeralds.Count() == 0 || monsters.MonstersLeftCount() == 0) && diggers.IsAnyAlive();
            gameData[currentPlayer].isLevelDone = isLevelDone;
        }

        public void IncrementPenalty()
        {
            ++penalty;
        }

        public void ClearTopLine()
        {
            drawing.EraseText(26, 0, 0, 3);
            drawing.EraseText(1, 308, 0, 3);
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
                sound.SetT2Val(40);
                sound.SetSoundT2();
                ClearTopLine();
                drawing.TextOut("PRESS ANY KEY", 80, 0, 1);
                NewFrame();
                input.GetKey(true);
                ClearTopLine();
                scores.DrawScores();
                for (int i = 0; i < diggerCount; i++)
                    scores.AddScore(i, 0);

                diggers.DrawLives();
                input.pausef = false;
            }
            else
                sound.SoundPauseOff();
        }

        private int GetArgument(char argch, string allargs, ref bool hasopt)
        {
            if (char.IsLetterOrDigit(argch))
                argch = char.ToUpper(argch);

            char c = argch;

            for (int i = 0; i < allargs.Length; ++i)
            {
                char cp = allargs[i];
                if (c == cp)
                {
                    hasopt = allargs.Length > i + 1 && allargs[i + 1] == ':';
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
