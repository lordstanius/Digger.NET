/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;
using System.IO;
using System.Text;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        struct scdat_struct
        {
            public int score, nextbs;
        }

        static scdat_struct[] scdat = new scdat_struct[DIGGERS];

        public static string highbuf;

        public static int[] scorehigh = new int[12];

        public static string[] scoreinit = new string[11];

        public static int scoret = 0;

        public static string hsbuf;

        public static byte[] scorebuf = new byte[512];

        public static int bonusscore = 20000;

        public static bool gotinitflag = false;

        public const string SFNAME = "DIGGER.SCO";

        static void readscores()
        {
            if (!levfflag)
            {
                try
                {
                    using (var inFile = File.OpenRead(SFNAME))
                    {
                        if (inFile.Read(scorebuf, 0, 512) == 0)
                            scorebuf[0] = 0;
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
            else
            {
                using (var inFile = File.OpenRead(levfname))
                {
                    inFile.Seek(1202, SeekOrigin.Begin);
                    if (inFile.Read(scorebuf, 0, 512) == 0)
                        scorebuf[0] = 0;
                }
            }
        }

        static void writescores()
        {
            if (!levfflag)
            {
                using (var inFile = File.OpenWrite(SFNAME))
                {
                    inFile.Write(scorebuf, 0, 512);
                }
            }
            else
            {
                using (var inFile = File.OpenRead(levfname))
                {
                    inFile.Seek(1202, SeekOrigin.Begin);
                    inFile.Write(scorebuf, 0, 512);
                }
            }
        }

        public static void initscores(digger_draw_api ddap)
        {
            int i;
            for (i = 0; i < diggers; i++)
                addscore(ddap, i, 0);
        }

        public static void loadscores()
        {
            int p = 0;
            readscores();
            if (gauntlet)
                p = 111;
            if (diggers == 2)
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
                    scorehigh[i + 1] = int.Parse(highbuf);
                }
            }
        }

        public static void zeroscores()
        {
            scdat[0].score = scdat[1].score = 0;
            scdat[0].nextbs = scdat[1].nextbs = bonusscore;
            scoret = 0;
        }

        public static void writecurscore(digger_draw_api ddap, int col)
        {
            if (curplayer == 0)
                writenum(ddap, scdat[0].score, 0, 0, 6, col);
            else
              if (scdat[1].score < 100000)
                writenum(ddap, scdat[1].score, 236, 0, 6, col);
            else
                writenum(ddap, scdat[1].score, 248, 0, 6, col);
        }

        public static void drawscores(digger_draw_api ddap)
        {
            writenum(ddap, scdat[0].score, 0, 0, 6, 3);
            if (nplayers == 2 || diggers == 2)
            {
                if (scdat[1].score < 100000)
                    writenum(ddap, scdat[1].score, 236, 0, 6, 3);
                else
                    writenum(ddap, scdat[1].score, 248, 0, 6, 3);
            }
        }

        public static void addscore(digger_draw_api ddap, int n, int score)
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
                if (getlives(n) < 5 || unlimlives)
                {
                    if (gauntlet)
                        cgtime += 17897715; /* 15 second time bonus instead of the life */
                    else
                        addlife(n);
                    drawlives(ddap);
                }
                scdat[n].nextbs += bonusscore;
            }
            incpenalty();
            incpenalty();
            incpenalty();
        }

        public static void endofgame(digger_draw_api ddap)
        {
            bool initflag = false;
            for (int i = 0; i < diggers; i++)
                addscore(ddap, i, 0);
            if (playing || !drfvalid)
                return;

            if (gauntlet)
            {
                cleartopline();
                outtext(ddap, "TIME UP", 120, 0, 3);
                for (int i = 0; i < 50 && !escape; i++)
                    newframe();
                erasetext(ddap, 7, 120, 0, 3);
            }
            for (int i = curplayer; i < curplayer + diggers; i++)
            {
                scoret = scdat[i].score;
                if (scoret > scorehigh[11])
                {
                    ddap.clear();
                    drawscores(ddap);
                    pldispbuf = $"PLAYER {(i == 0 ? 1 : 2)}";
                    outtext(ddap, pldispbuf, 108, 0, 2);
                    outtext(ddap, " NEW HIGH SCORE ", 64, 40, 2);
                    getinitials(ddap);
                    shufflehigh();
                    savescores();
                    initflag = true;
                }
            }
            if (!initflag && !gauntlet)
            {
                cleartopline();
                outtext(ddap, "GAME OVER", 104, 0, 3);
                for (int i = 0; i < 50 && !escape; i++)
                    newframe();
                erasetext(ddap, 9, 104, 0, 3);
                setretr(true);
            }
        }

        public static void showtable(digger_draw_api ddap)
        {
            int i, col;
            outtext(ddap, "HIGH SCORES", 16, 25, 3);
            col = 2;
            for (i = 1; i < 11; i++)
            {
                highbuf = numtostring(scorehigh[i + 1]);
                hsbuf = $"{scoreinit[i]}  {highbuf}";
                outtext(ddap, hsbuf, 16, 31 + 13 * i, col);
                col = 1;
            }
        }

        static void savescores()
        {
            int i, p = 0, j;
            if (gauntlet)
                p = 111;
            if (diggers == 2)
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

        public static void getinitials(digger_draw_api ddap)
        {
            int k, i;
            newframe();
            outtext(ddap, "ENTER YOUR", 100, 70, 3);
            outtext(ddap, " INITIALS", 100, 90, 3);
            outtext(ddap, "_ _ _", 128, 130, 3);
            scoreinit[0] = "...";
            killsound();
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
                    ddap.write(i * 24 + 128, 130, (char)k, 3);
                    initials[i] = (char)k;
                }
            }
            scoreinit[0] = new string(initials);
            for (i = 0; i < 20; i++)
                flashywait(ddap, 15);

            setupsound();
            ddap.clear();
            ddap.pal(0);
            ddap.inten(0);
            setretr(true);
            recputinit(scoreinit[0]);
        }

        public static void flashywait(digger_draw_api ddap, int n)
        {
            int i, gt, cx;
            int p = 0;
            byte gap = 19;

            gethrt();
            setretr(false);
            for (i = 0; i < (n << 1); i++)
                for (cx = 0; cx < volume; cx++)
                {
                    p = 1 - p;
                    ddap.pal(p);
                    ddap.flush();
                    for (gt = 0; gt < gap; gt++) ;
                }
        }

        public static int getinitial(digger_draw_api ddap, int x, int y)
        {
            int i;
            ddap.write(x, y, '_', 3);
            do
            {
                for (i = 0; i < 40; i++)
                {
                    if (kbhit())
                    {
                        int key = getkey(false);
                        if (!char.IsLetterOrDigit((char)key))
                            continue;
                        return key;
                    }
                    flashywait(ddap, 15);
                }
                for (i = 0; i < 40; i++)
                {
                    if (kbhit())
                    {
                        ddap.write(x, y, '_', 3);
                        return getkey(false);
                    }
                    flashywait(ddap, 15);
                }
            } while (true);
        }

        static void shufflehigh()
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

        public static void scorekill(digger_draw_api ddap, int n)
        {
            addscore(ddap, n, 250);
        }

        public static void scorekill2(digger_draw_api ddap)
        {
            addscore(ddap, 0, 125);
            addscore(ddap, 1, 125);
        }

        public static void scoreemerald(digger_draw_api ddap, int n)
        {
            addscore(ddap, n, 25);
        }

        public static void scoreoctave(digger_draw_api ddap, int n)
        {
            addscore(ddap, n, 250);
        }

        public static void scoregold(digger_draw_api ddap, int n)
        {
            addscore(ddap, n, 500);
        }

        public static void scorebonus(digger_draw_api ddap, int n)
        {
            addscore(ddap, n, 1000);
        }

        public static void scoreeatm(digger_draw_api ddap, int n, int msc)
        {
            addscore(ddap, n, msc * 200);
        }

        static void writenum(digger_draw_api ddap, int n, int x, int y, int w, int c)
        {
            int xp = (w - 1) * 12 + x;
            while (w > 0)
            {
                int d = n % 10;
                if (w > 1 || d > 0)
                    ddap.write(xp, y, (char)(d + '0'), c);
                n /= 10;
                w--;
                xp -= 12;
            }
        }

        static string numtostring(int n)
        {
            return string.Format("{0,-6:d}", n);
            //int x;
            //var p = new char[7];
            //for (x = 0; x < 6; x++)
            //{
            //    p[5 - x] = (char)((n % 10) + '0');
            //    n /= 10;
            //    if (n == 0)
            //    {
            //        x++;
            //        break;
            //    }
            //}
            //for (; x < 6; x++)
            //    p[5 - x] = ' ';
            //p[6] = '\0';
        }
    }
}