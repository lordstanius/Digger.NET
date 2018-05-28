/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;
using System.IO;
using System.Text;

namespace Digger.Net
{
    public class Scores
    {
        struct scdat_struct
        {
            public int score, nextbs;
        }

        static scdat_struct[] scdat = new scdat_struct[Const.DIGGERS];

        public string highbuf;

        public int[] scorehigh = new int[12];

        public string[] scoreinit = new string[11];

        public int scoret = 0;

        public string hsbuf;

        public byte[] scorebuf = new byte[512];

        public int bonusscore = 20000;

        public bool gotinitflag = false;

        public const string SFNAME = "DIGGER.SCO";

        private readonly Game game;
        private readonly Video video;

        public Scores(Game game)
        {
            this.game = game;
            this.video = game.video;
        }

        private void readscores()
        {
            if (!game.level.IsUsingLevelFile)
            {
                if (File.Exists(SFNAME))
                {
                    using (var inFile = File.OpenRead(SFNAME))
                    {
                        if (inFile.Read(scorebuf, 0, 512) == 0)
                            scorebuf[0] = 0;
                    }
                }
            }
            else
            {
                using (var inFile = File.OpenRead(game.level.LevelFileName))
                {
                    inFile.Seek(1202, SeekOrigin.Begin);
                    if (inFile.Read(scorebuf, 0, 512) == 0)
                        scorebuf[0] = 0;
                }
            }
        }

        private void writescores()
        {
            if (!game.level.IsUsingLevelFile)
            {
                using (var inFile = File.OpenWrite(SFNAME))
                {
                    inFile.Write(scorebuf, 0, 512);
                }
            }
            else
            {
                using (var inFile = File.OpenRead(game.level.LevelFileName))
                {
                    inFile.Seek(1202, SeekOrigin.Begin);
                    inFile.Write(scorebuf, 0, 512);
                }
            }
        }

        public void initscores()
        {
            int i;
            for (i = 0; i < game.DiggerCount; i++)
                AddScore(i, 0);
        }

        public void loadscores()
        {
            int p = 0;
            readscores();
            if (game.IsGauntletMode)
                p = 111;
            if (game.DiggerCount == 2)
                p += 222;
            if (scorebuf[p++] != 's')
            {
                for (int i = 0; i < 11; i++)
                {
                    scorehigh[i + 1] = 0;
                    scoreinit[i] = "...";
                }
            }
            else
            {
                for (int i = 1; i < 11; i++)
                {
                    scoreinit[i] = Encoding.ASCII.GetString(scorebuf, p, 3);
                    p += 5;
                    highbuf = Encoding.ASCII.GetString(scorebuf, p, 6);
                    if (int.TryParse(highbuf.TrimEnd(), out int highScore))
                        scorehigh[i + 1] = highScore;
                    p += 6;
                }
            }
        }

        public void zeroscores()
        {
            scdat[0].score = scdat[1].score = 0;
            scdat[0].nextbs = scdat[1].nextbs = bonusscore;
            scoret = 0;
        }

        public void writecurscore(int col)
        {
            if (game.CurrentPlayer == 0)
                writenum(scdat[0].score, 0, 0, 6, col);
            else
              if (scdat[1].score < 100000)
                writenum(scdat[1].score, 236, 0, 6, col);
            else
                writenum(scdat[1].score, 248, 0, 6, col);
        }

        public void drawscores()
        {
            writenum(scdat[0].score, 0, 0, 6, 3);
            if (game.PlayerCount == 2 || game.DiggerCount == 2)
            {
                if (scdat[1].score < 100000)
                    writenum(scdat[1].score, 236, 0, 6, 3);
                else
                    writenum(scdat[1].score, 248, 0, 6, 3);
            }
        }

        public void AddScore(int n, int score)
        {
            scdat[n].score += score;
            if (scdat[n].score > 999999)
                scdat[n].score = 0;
            if (n == 0)
                writenum(scdat[n].score, 0, 0, 6, 1);
            else
              if (scdat[n].score < 100000)
                writenum(scdat[n].score, 236, 0, 6, 1);
            else
                writenum(scdat[n].score, 248, 0, 6, 1);
            if (scdat[n].score >= scdat[n].nextbs + n)
            { /* +n to reproduce original bug */
                if (game.diggers.GetLives(n) < 5 || game.HasUnlimitedLives)
                {
                    if (game.IsGauntletMode)
                        game.diggers.cgtime += 17897715; /* 15 second time bonus instead of the life */
                    else
                        game.diggers.addlife(n);
                    game.diggers.DrawLives();
                }
                scdat[n].nextbs += bonusscore;
            }
            game.IncreasePenalty();
            game.IncreasePenalty();
            game.IncreasePenalty();
        }

        public void endofgame()
        {
            bool initflag = false;
            for (int i = 0; i < game.DiggerCount; i++)
                AddScore(i, 0);
            if (game.record.IsPlaying || !game.record.IsDrfValid)
                return;

            if (game.IsGauntletMode)
            {
                game.ClearTopLine();
                game.video.TextOut("TIME UP", 120, 0, 3);
                for (int i = 0; i < 50 && !game.input.IsGameCycleEnded; i++)
                    game.NewFrame();
                video.EraseText(7, 120, 0, 3);
            }
            for (int i = game.CurrentPlayer; i < game.CurrentPlayer + game.DiggerCount; i++)
            {
                scoret = scdat[i].score;
                if (scoret > scorehigh[11])
                {
                    video.Clear();
                    drawscores();
                    game.PlayerName = $"PLAYER {(i == 0 ? 1 : 2)}";
                    video.TextOut(game.PlayerName, 108, 0, 2);
                    video.TextOut(" NEW HIGH SCORE ", 64, 40, 2);
                    GetInitials();
                    ShuffleHigh();
                    savescores();
                    initflag = true;
                }
            }
            if (!initflag && !game.IsGauntletMode)
            {
                game.ClearTopLine();
                video.TextOut("GAME OVER", 104, 0, 3);
                for (int i = 0; i < 50 && !game.input.IsGameCycleEnded; i++)
                    game.NewFrame();
                video.EraseText(9, 104, 0, 3);
            }
        }

        public void ShowTable()
        {
            int i, col;
            video.TextOut("HIGH SCORES", 16, 25, 3);
            col = 2;
            for (i = 1; i < 11; i++)
            {
                highbuf = numtostring(scorehigh[i + 1]);
                hsbuf = $"{scoreinit[i]}  {highbuf}";
                video.TextOut(hsbuf, 16, 31 + 13 * i, col);
                col = 1;
            }
        }

        private void savescores()
        {
            int i, p = 0, j;
            if (game.IsGauntletMode)
                p = 111;
            if (game.DiggerCount == 2)
                p += 222;
            scorebuf[p] = (byte)'s';
            for (i = 1; i < 11; i++)
            {
                highbuf = numtostring(scorehigh[i + 1]);
                hsbuf = $"{scoreinit[i]}  {highbuf}";
                for (j = 0; j < 11; j++)
                    scorebuf[p + j + i * 11 - 10] = (byte)hsbuf[j];
            }
            writescores();
        }

        public void GetInitials()
        {
            int k, i;
            game.NewFrame();
            video.TextOut("ENTER YOUR", 100, 70, 3);
            video.TextOut(" INITIALS", 100, 90, 3);
            video.TextOut("_ _ _", 128, 130, 3);
            scoreinit[0] = "...";
            game.sound.KillSound();
            var initials = new char[3];
            for (i = 0; i < 3; i++)
            {
                k = 0;
                while (k == 0)
                {
                    k = GetInitial(i * 24 + 128, 130);
                    if (k == 8 || k == 127)
                    {
                        if (i > 0)
                            i--;
                        k = 0;
                    }
                }
                if (k != 0)
                {
                    video.WriteChar(i * 24 + 128, 130, (char)k, 3);
                    initials[i] = (char)k;
                }
            }
            scoreinit[0] = new string(initials);
            for (i = 0; i < 20; i++)
                FlashyWait(15);

            game.sound.SetupSound();
            video.Clear();
            video.ResetPalette();
            game.record.PutInitials(scoreinit[0]);
        }

        public void FlashyWait(int n)
        {
            game.timer.SyncFrame();

            video.FlashyWait(n);
        }

        public int GetInitial(int x, int y)
        {
            video.WriteChar(x, y, '_', 3);

            do
            {
                for (int i = 0; i < 40; i++)
                {
                    if (game.input.keyboard.IsKeyboardHit())
                    {
                        int key = game.input.keyboard.GetKey(false);
                        if (!char.IsLetterOrDigit((char)key))
                            continue;
                        return key;
                    }
                    FlashyWait(15);
                }

                for (int i = 0; i < 40; i++)
                {
                    if (game.input.keyboard.IsKeyboardHit())
                    {
                        video.WriteChar(x, y, '_', 3);
                        return game.input.keyboard.GetKey(false);
                    }
                    FlashyWait(15);
                }
            } while (true);
        }

        private void ShuffleHigh()
        {
            int i, j;
            for (j = 10; j > 1; j--)
                if (scoret < scorehigh[j])
                    break;
            for (i = 10; i > j; i--)
            {
                scorehigh[i + 1] = scorehigh[i];
                scoreinit[i] = scoreinit[i - 1];
            }
            scorehigh[j + 1] = scoret;
            scoreinit[j] = scoreinit[0];
        }

        public void ScoreKill(int n)
        {
            AddScore(n, 250);
        }

        public void ScoreKill()
        {
            AddScore(0, 125);
            AddScore(1, 125);
        }

        public void ScoreEmerald(int n)
        {
            AddScore(n, 25);
        }

        public void ScoreOctave(int n)
        {
            AddScore(n, 250);
        }

        public void scoregold(int n)
        {
            AddScore(n, 500);
        }

        public void ScoreBonus(int n)
        {
            AddScore(n, 1000);
        }

        public void ScoreEatMonster(int n, int msc)
        {
            AddScore(n, msc * 200);
        }

        private void writenum(int n, int x, int y, int w, int c)
        {
            int xp = (w - 1) * 12 + x;
            while (w > 0)
            {
                int d = n % 10;
                if (w > 1 || d > 0)
                    video.WriteChar(xp, y, (char)(d + '0'), c);

                n /= 10;
                w--;
                xp -= 12;
            }
        }

        static string numtostring(int n)
        {
            return string.Format("{0,-6:d}", n);
        }
    }
}