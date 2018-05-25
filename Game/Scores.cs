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

        static scdat_struct[] scdat = new scdat_struct[DiggerC.DIGGERS];

        public string highbuf;

        public int[] scorehigh = new int[12];

        public string[] scoreinit = new string[11];

        public int scoret = 0;

        public string hsbuf;

        public byte[] scorebuf = new byte[512];

        public int bonusscore = 20000;

        public bool gotinitflag = false;

        public const string SFNAME = "DIGGER.SCO";

        private void readscores()
        {
            if (!DiggerC.level.levfflag)
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
                using (var inFile = File.OpenRead(DiggerC.level.levfname))
                {
                    inFile.Seek(1202, SeekOrigin.Begin);
                    if (inFile.Read(scorebuf, 0, 512) == 0)
                        scorebuf[0] = 0;
                }
            }
        }

        private void writescores()
        {
            if (!DiggerC.level.levfflag)
            {
                using (var inFile = File.OpenWrite(SFNAME))
                {
                    inFile.Write(scorebuf, 0, 512);
                }
            }
            else
            {
                using (var inFile = File.OpenRead(DiggerC.level.levfname))
                {
                    inFile.Seek(1202, SeekOrigin.Begin);
                    inFile.Write(scorebuf, 0, 512);
                }
            }
        }

        public void initscores(SdlGraphics ddap)
        {
            int i;
            for (i = 0; i < DiggerC.g_Diggers; i++)
                addscore(ddap, i, 0);
        }

        public void loadscores()
        {
            int p = 0;
            readscores();
            if (DiggerC.g_isGauntletMode)
                p = 111;
            if (DiggerC.g_Diggers == 2)
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

        public void writecurscore(SdlGraphics ddap, int col)
        {
            if (DiggerC.g_CurrentPlayer == 0)
                writenum(ddap, scdat[0].score, 0, 0, 6, col);
            else
              if (scdat[1].score < 100000)
                writenum(ddap, scdat[1].score, 236, 0, 6, col);
            else
                writenum(ddap, scdat[1].score, 248, 0, 6, col);
        }

        public void drawscores(SdlGraphics ddap)
        {
            writenum(ddap, scdat[0].score, 0, 0, 6, 3);
            if (DiggerC.g_playerCount == 2 || DiggerC.g_Diggers == 2)
            {
                if (scdat[1].score < 100000)
                    writenum(ddap, scdat[1].score, 236, 0, 6, 3);
                else
                    writenum(ddap, scdat[1].score, 248, 0, 6, 3);
            }
        }

        public void addscore(SdlGraphics ddap, int n, int score)
        {
            scdat[n].score += score;
            if (scdat[n].score > 999999)
                scdat[n].score = 0;
            if (n == 0)
                writenum(ddap, scdat[n].score, 0, 0, 6, 1);
            else
              if (scdat[n].score < 100000)
                writenum(ddap, scdat[n].score, 236, 0, 6, 1);
            else
                writenum(ddap, scdat[n].score, 248, 0, 6, 1);
            if (scdat[n].score >= scdat[n].nextbs + n)
            { /* +n to reproduce original bug */
                if (DiggerC.getlives(n) < 5 || DiggerC.g_hasUnlimitedLives)
                {
                    if (DiggerC.g_isGauntletMode)
                        DiggerC.cgtime += 17897715; /* 15 second time bonus instead of the life */
                    else
                        DiggerC.addlife(n);
                    DiggerC.drawApi.drawlives(ddap);
                }
                scdat[n].nextbs += bonusscore;
            }
            DiggerC.incpenalty();
            DiggerC.incpenalty();
            DiggerC.incpenalty();
        }

        public void endofgame(SdlGraphics ddap)
        {
            bool initflag = false;
            for (int i = 0; i < DiggerC.g_Diggers; i++)
                addscore(ddap, i, 0);
            if (DiggerC.record.playing || !DiggerC.record.drfvalid)
                return;

            if (DiggerC.g_isGauntletMode)
            {
                DiggerC.cleartopline();
                DiggerC.drawApi.TextOut(ddap, "TIME UP", 120, 0, 3);
                for (int i = 0; i < 50 && !DiggerC.input.escape; i++)
                    DiggerC.newframe();
                DiggerC.drawApi.EraseText(ddap, 7, 120, 0, 3);
            }
            for (int i = DiggerC.g_CurrentPlayer; i < DiggerC.g_CurrentPlayer + DiggerC.g_Diggers; i++)
            {
                scoret = scdat[i].score;
                if (scoret > scorehigh[11])
                {
                    ddap.Clear();
                    drawscores(ddap);
                    DiggerC.g_playerName = $"PLAYER {(i == 0 ? 1 : 2)}";
                    DiggerC.drawApi.TextOut(ddap, DiggerC.g_playerName, 108, 0, 2);
                    DiggerC.drawApi.TextOut(ddap, " NEW HIGH SCORE ", 64, 40, 2);
                    getinitials(ddap);
                    shufflehigh();
                    savescores();
                    initflag = true;
                }
            }
            if (!initflag && !DiggerC.g_isGauntletMode)
            {
                DiggerC.cleartopline();
                DiggerC.drawApi.TextOut(ddap, "GAME OVER", 104, 0, 3);
                for (int i = 0; i < 50 && !DiggerC.input.escape; i++)
                    DiggerC.newframe();
                DiggerC.drawApi.EraseText(ddap, 9, 104, 0, 3);
            }
        }

        public void showtable(SdlGraphics ddap)
        {
            int i, col;
            DiggerC.drawApi.TextOut(ddap, "HIGH SCORES", 16, 25, 3);
            col = 2;
            for (i = 1; i < 11; i++)
            {
                highbuf = numtostring(scorehigh[i + 1]);
                hsbuf = $"{scoreinit[i]}  {highbuf}";
                DiggerC.drawApi.TextOut(ddap, hsbuf, 16, 31 + 13 * i, col);
                col = 1;
            }
        }

        private void savescores()
        {
            int i, p = 0, j;
            if (DiggerC.g_isGauntletMode)
                p = 111;
            if (DiggerC.g_Diggers == 2)
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

        public void getinitials(SdlGraphics ddap)
        {
            int k, i;
            DiggerC.newframe();
            DiggerC.drawApi.TextOut(ddap, "ENTER YOUR", 100, 70, 3);
            DiggerC.drawApi.TextOut(ddap, " INITIALS", 100, 90, 3);
            DiggerC.drawApi.TextOut(ddap, "_ _ _", 128, 130, 3);
            scoreinit[0] = "...";
            DiggerC.KillSound();
            var initials = new char[3];
            for (i = 0; i < 3; i++)
            {
                k = 0;
                while (k == 0)
                {
                    k = getinitial(ddap, i * 24 + 128, 130);
                    if (k == 8 || k == 127)
                    {
                        if (i > 0)
                            i--;
                        k = 0;
                    }
                }
                if (k != 0)
                {
                    ddap.WriteChar(i * 24 + 128, 130, (char)k, 3);
                    initials[i] = (char)k;
                }
            }
            scoreinit[0] = new string(initials);
            for (i = 0; i < 20; i++)
                flashywait(ddap, 15);

            DiggerC.SetupSound();
            ddap.Clear();
            ddap.SetPalette(0);
            ddap.SetIntensity(0);
            DiggerC.record.recputinit(scoreinit[0]);
        }

        public void flashywait(SdlGraphics ddap, int n)
        {
            int i, gt, cx;
            int p = 0;
            byte gap = 19;

            DiggerC.timer.SyncFrame();
            DiggerC.sdlGfx.UpdateScreen();

            for (i = 0; i < (n << 1); i++)
                for (cx = 0; cx < DiggerC.volume; cx++)
                {
                    p = 1 - p;
                    ddap.SetPalette(p);
                    for (gt = 0; gt < gap; gt++) ;
                }
        }

        public int getinitial(SdlGraphics ddap, int x, int y)
        {
            int i;
            ddap.WriteChar(x, y, '_', 3);
            do
            {
                for (i = 0; i < 40; i++)
                {
                    if (DiggerC.input.keyboard.IsKeyboardHit())
                    {
                        int key = DiggerC.input.keyboard.GetKey(false);
                        if (!char.IsLetterOrDigit((char)key))
                            continue;
                        return key;
                    }
                    flashywait(ddap, 15);
                }
                for (i = 0; i < 40; i++)
                {
                    if (DiggerC.input.keyboard.IsKeyboardHit())
                    {
                        ddap.WriteChar(x, y, '_', 3);
                        return DiggerC.input.keyboard.GetKey(false);
                    }
                    flashywait(ddap, 15);
                }
            } while (true);
        }

        private void shufflehigh()
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

        public void scorekill(SdlGraphics ddap, int n)
        {
            addscore(ddap, n, 250);
        }

        public void scorekill2(SdlGraphics ddap)
        {
            addscore(ddap, 0, 125);
            addscore(ddap, 1, 125);
        }

        public void scoreemerald(SdlGraphics ddap, int n)
        {
            addscore(ddap, n, 25);
        }

        public void scoreoctave(SdlGraphics ddap, int n)
        {
            addscore(ddap, n, 250);
        }

        public void scoregold(SdlGraphics ddap, int n)
        {
            addscore(ddap, n, 500);
        }

        public void scorebonus(SdlGraphics ddap, int n)
        {
            addscore(ddap, n, 1000);
        }

        public void scoreeatm(SdlGraphics ddap, int n, int msc)
        {
            addscore(ddap, n, msc * 200);
        }

        static void writenum(SdlGraphics ddap, int n, int x, int y, int w, int c)
        {
            int xp = (w - 1) * 12 + x;
            while (w > 0)
            {
                int d = n % 10;
                if (w > 1 || d > 0)
                    ddap.WriteChar(xp, y, (char)(d + '0'), c);
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