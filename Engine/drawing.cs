/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using SDL2;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public const int MAX_W = 320;
        public const int MAX_H = 200;
        public const int CHR_W = 12;
        public const int CHR_H = 12;

        public const int MAX_TEXT_LEN = MAX_W / CHR_W;

        public static int[] field1 = new int[MSIZE];
        public static int[] field2 = new int[MSIZE];
        public static int[] field = new int[MSIZE];

        public static Surface[] monbufs = new Surface[MONSTERS];
        public static Surface[] bagbufs = new Surface[BAGS];
        public static Surface[] bonusbufs = new Surface[BONUSES];
        public static Surface[] diggerbufs = new Surface[DIGGERS];
        public static Surface[] firebufs = new Surface[FIREBALLS];

        public static ushort[] bitmasks = { 0xfffe, 0xfffd, 0xfffb, 0xfff7, 0xffef, 0xffdf, 0xffbf, 0xff7f, 0xfeff, 0xfdff, 0xfbff, 0xf7ff };

        public static int[] digspr = new int[DIGGERS];
        public static int[] digspd = new int[DIGGERS];
        public static int[] firespr = new int[FIREBALLS];

        public static readonly string empty_line = new string(' ', MAX_TEXT_LEN + 1);

        private static void outtextl(digger_draw_api ddap, string p, int x, int y, int c, int l)
        {
#if DIGGER_DEBUG
  System.Diagnostics.Debug.Assert(l > 0 && l <= MAX_TEXT_LEN);
#endif
            for (int i = 0; i < l; i++)
            {
                ddap.write(x, y, isvalchar(p[i]) ? p[i] : ' ', c);
                x += CHR_W;
            }
        }

        public static void outtext(digger_draw_api ddap, string p, int x, int y, int c)
        {
            outtextl(ddap, p, x, y, c, p.Length);
        }

        public static void erasetext(digger_draw_api ddap, short n, int x, int y, short c)
        {
            outtextl(ddap, empty_line, x, y, c, n);
        }

        public static void makefield()
        {
            for (int x = 0; x < MWIDTH; x++)
            {
                for (int y = 0; y < MHEIGHT; y++)
                {
                    field[y * MWIDTH + x] = -1;
                    char c = getlevch(x, y, levplan());
                    if (c == 'S' || c == 'V')
                        field[y * MWIDTH + x] &= 0xd03f;
                    if (c == 'S' || c == 'H')
                        field[y * MWIDTH + x] &= 0xdfe0;
                    if (curplayer == 0)
                        field1[y * MWIDTH + x] = field[y * MWIDTH + x];
                    else
                        field2[y * MWIDTH + x] = field[y * MWIDTH + x];
                }
            }
        }

        public static void drawstatics(digger_draw_api ddap)
        {
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (curplayer == 0)
                        field[y * MWIDTH + x] = field1[y * MWIDTH + x];
                    else
                        field[y * MWIDTH + x] = field2[y * MWIDTH + x];
            setretr(true);
            ddap.pal(0);
            ddap.inten(0);
            drawbackg(levplan());
            drawfield();
        }

        public static void savefield()
        {
            int x, y;
            for (x = 0; x < MWIDTH; x++)
                for (y = 0; y < MHEIGHT; y++)
                    if (curplayer == 0)
                        field1[y * MWIDTH + x] = field[y * MWIDTH + x];
                    else
                        field2[y * MWIDTH + x] = field[y * MWIDTH + x];
        }

        public static void drawfield()
        {
            int x, y, xp, yp;
            for (x = 0; x < MWIDTH; x++)
                for (y = 0; y < MHEIGHT; y++)
                    if ((field[y * MWIDTH + x] & 0x2000) == 0)
                    {
                        xp = x * 20 + 12;
                        yp = y * 18 + 18;
                        if ((field[y * MWIDTH + x] & 0xfc0) != 0xfc0)
                        {
                            field[y * MWIDTH + x] &= 0xd03f;
                            drawbottomblob(xp, yp - 15);
                            drawbottomblob(xp, yp - 12);
                            drawbottomblob(xp, yp - 9);
                            drawbottomblob(xp, yp - 6);
                            drawbottomblob(xp, yp - 3);
                            drawtopblob(xp, yp + 3);
                        }
                        if ((field[y * MWIDTH + x] & 0x1f) != 0x1f)
                        {
                            field[y * MWIDTH + x] &= 0xdfe0;
                            drawrightblob(xp - 16, yp);
                            drawrightblob(xp - 12, yp);
                            drawrightblob(xp - 8, yp);
                            drawrightblob(xp - 4, yp);
                            drawleftblob(xp + 4, yp);
                        }
                        if (x < 14)
                            if ((field[y * MWIDTH + x + 1] & 0xfdf) != 0xfdf)
                                drawrightblob(xp, yp);
                        if (y < 9)
                            if ((field[(y + 1) * MWIDTH + x] & 0xfdf) != 0xfdf)
                                drawbottomblob(xp, yp);
                    }
        }

        public static void eatfield(int x, int y, int dir)
        {
            int h = (x - 12) / 20, xr = ((x - 12) % 20) / 4, v = (y - 18) / 18, yr = ((y - 18) % 18) / 3;
            incpenalty();
            switch (dir)
            {
                case DIR_RIGHT:
                    h++;
                    field[v * MWIDTH + h] &= bitmasks[xr];
                    if ((field[v * MWIDTH + h] & 0x1f) != 0)
                        break;
                    field[v * MWIDTH + h] &= 0xdfff;
                    break;
                case DIR_UP:
                    yr--;
                    if (yr < 0)
                    {
                        yr += 6;
                        v--;
                    }
                    field[v * MWIDTH + h] &= bitmasks[6 + yr];
                    if ((field[v * MWIDTH + h] & 0xfc0) != 0)
                        break;
                    field[v * MWIDTH + h] &= 0xdfff;
                    break;
                case DIR_LEFT:
                    xr--;
                    if (xr < 0)
                    {
                        xr += 5;
                        h--;
                    }
                    field[v * MWIDTH + h] &= bitmasks[xr];
                    if ((field[v * MWIDTH + h] & 0x1f) != 0)
                        break;
                    field[v * MWIDTH + h] &= 0xdfff;
                    break;
                case DIR_DOWN:
                    v++;
                    field[v * MWIDTH + h] &= bitmasks[6 + yr];
                    if ((field[v * MWIDTH + h] & 0xfc0) != 0)
                        break;
                    field[v * MWIDTH + h] &= 0xdfff;
                    break;
            }
        }

        public static void creatembspr()
        {
            for (int i = 0; i < BAGS; i++)
                createspr(FIRSTBAG + i, 62, bagbufs[i], 4, 15, 0, 0);
            for (int i = 0; i < MONSTERS; i++)
                createspr(FIRSTMONSTER + i, 71, monbufs[i], 4, 15, 0, 0);
            createdbfspr();
        }

        public static void initmbspr()
        {
            for (int i = 0; i < BAGS; i++)
                initspr(FIRSTBAG + i, 62, 4, 15, 0, 0);

            for (int i = 0; i < MONSTERS; i++)
                initspr(FIRSTMONSTER + i, 71, 4, 15, 0, 0);
            initdbfspr();
        }

        public static void drawgold(int n, int t, int x, int y)
        {
            initspr(FIRSTBAG + n, t + 62, 4, 15, 0, 0);
            drawspr(FIRSTBAG + n, x, y);
        }

        public static void drawlife(int t, int x, int y)
        {
            drawmiscspr(x, y, t + 110, 4, 12);
        }

        public static void drawemerald(int x, int y)
        {
            initmiscspr(x, y, 4, 10);
            drawmiscspr(x, y, 108, 4, 10);
            getis();
        }

        public static void eraseemerald(int x, int y)
        {
            initmiscspr(x, y, 4, 10);
            drawmiscspr(x, y, 109, 4, 10);
            getis();
        }

        public static void createdbfspr()
        {
            int i;
            for (i = 0; i < DIGGERS; i++)
            {
                digspd[i] = 1;
                digspr[i] = 0;
            }
            for (i = 0; i < FIREBALLS; i++)
                firespr[i] = 0;
            for (i = FIRSTDIGGER; i < LASTDIGGER; i++)
                createspr(i, 0, diggerbufs[i - FIRSTDIGGER], 4, 15, 0, 0);
            for (i = FIRSTBONUS; i < LASTBONUS; i++)
                createspr(i, 81, bonusbufs[i - FIRSTBONUS], 4, 15, 0, 0);
            for (i = FIRSTFIREBALL; i < LASTFIREBALL; i++)
                createspr(i, 82, firebufs[i - FIRSTFIREBALL], 2, 8, 0, 0);
        }

        public static void initdbfspr()
        {
            int i;
            for (i = 0; i < DIGGERS; i++)
            {
                digspd[i] = 1;
                digspr[i] = 0;
            }
            for (i = 0; i < FIREBALLS; i++)
                firespr[i] = 0;
            for (i = FIRSTDIGGER; i < LASTDIGGER; i++)
                initspr(i, 0, 4, 15, 0, 0);
            for (i = FIRSTBONUS; i < LASTBONUS; i++)
                initspr(i, 81, 4, 15, 0, 0);
            for (i = FIRSTFIREBALL; i < LASTFIREBALL; i++)
                initspr(i, 82, 2, 8, 0, 0);
        }

        public static void drawrightblob(int x, int y)
        {
            initmiscspr(x + 16, y - 1, 2, 18);
            drawmiscspr(x + 16, y - 1, 102, 2, 18);
            getis();
        }

        public static void drawleftblob(int x, int y)
        {
            initmiscspr(x - 8, y - 1, 2, 18);
            drawmiscspr(x - 8, y - 1, 104, 2, 18);
            getis();
        }

        public static void drawtopblob(int x, int y)
        {
            initmiscspr(x - 4, y - 6, 6, 6);
            drawmiscspr(x - 4, y - 6, 103, 6, 6);
            getis();
        }

        public static void drawbottomblob(int x, int y)
        {
            initmiscspr(x - 4, y + 15, 6, 6);
            drawmiscspr(x - 4, y + 15, 105, 6, 6);
            getis();
        }

        public static void drawfurryblob(int x, int y)
        {
            initmiscspr(x - 4, y + 15, 6, 8);
            drawmiscspr(x - 4, y + 15, 107, 6, 8);
            getis();
        }

        public static void drawsquareblob(int x, int y)
        {
            initmiscspr(x - 4, y + 17, 6, 6);
            drawmiscspr(x - 4, y + 17, 106, 6, 6);
            getis();
        }

        public static void drawbackg(int l)
        {
            for (int y = 14; y < 200; y += 4)
            {
                fillbuffer();
                for (int x = 0; x < 320; x += 20)
                    drawmiscspr(x, y, 93 + l, 5, 4);
            }
        }

        public static void drawfire(int n, int x, int y, int t)
        {
            int nn = (n == 0) ? 0 : 32;
            if (t == 0)
            {
                firespr[n]++;
                if (firespr[n] > 2)
                    firespr[n] = 0;
                initspr(FIRSTFIREBALL + n, 82 + firespr[n] + nn, 2, 8, 0, 0);
            }
            else
                initspr(FIRSTFIREBALL + n, 84 + t + nn, 2, 8, 0, 0);
            drawspr(FIRSTFIREBALL + n, x, y);
        }

        public static void drawbonus(int x, int y)
        {
            int n = 0;
            initspr(FIRSTBONUS + n, 81, 4, 15, 0, 0);
            movedrawspr(FIRSTBONUS + n, x, y);
        }

        public static void drawdigger(int n, int t, int x, int y, bool f)
        {
            int nn = (n == 0) ? 0 : 31;
            digspr[n] += digspd[n];
            if (digspr[n] == 2 || digspr[n] == 0)
                digspd[n] = -digspd[n];
            if (digspr[n] > 2)
                digspr[n] = 2;
            if (digspr[n] < 0)
                digspr[n] = 0;
            if (t >= 0 && t <= 6 && (t & 1) == 0)
            {
                initspr(FIRSTDIGGER + n, (t + (f ? 0 : 1)) * 3 + digspr[n] + 1 + nn, 4, 15, 0, 0);
                drawspr(FIRSTDIGGER + n, x, y);
                return;
            }
            if (t >= 10 && t <= 15)
            {
                initspr(FIRSTDIGGER + n, 40 + nn - t, 4, 15, 0, 0);
                drawspr(FIRSTDIGGER + n, x, y);
                return;
            }
            first[0] = first[1] = first[2] = first[3] = first[4] = -1;
        }

        public static void drawlives(digger_draw_api ddap)
        {
            int l, n, g;
            if (gauntlet)
            {
                g = (int)(cgtime / 1193181);
                string buf = string.Format("{0:D3}:{1:D2}", g / 60, g % 60);
                outtext(ddap, buf, 124, 0, 3);
                return;
            }
            n = getlives(0) - 1;
            erasetext(ddap, 5, 96, 0, 2);
            if (n > 4)
            {
                drawlife(0, 80, 0);
                string buf = string.Format("0x{0:X4}", n);
                outtext(ddap, buf, 100, 0, 2);
            }
            else
            {
                for (l = 1; l < 5; l++)
                {
                    drawlife(n > 0 ? 0 : 2, l * 20 + 60, 0);
                    n--;
                }
            }

            if (nplayers == 2)
            {
                erasetext(ddap, 5, 164, 0, 2);
                n = getlives(1) - 1;
                if (n > 4)
                {
                    string buf = string.Format("0x{0:X4}", n);
                    outtext(ddap, buf, 220 - buf.Length * CHR_W, 0, 2);
                    drawlife(1, 224, 0);
                }
                else
                {
                    for (l = 1; l < 5; l++)
                    {
                        drawlife(n > 0 ? 1 : 2, 244 - l * 20, 0);
                        n--;
                    }
                }
            }

            if (diggers == 2)
            {
                erasetext(ddap, 5, 164, 0, 1);
                n = getlives(1) - 1;
                if (n > 4)
                {
                    string buf = string.Format("0x{0:X4}", n);
                    outtext(ddap, buf, 220 - buf.Length * CHR_W, 0, 1);
                    drawlife(3, 224, 0);
                }
                else
                {
                    for (l = 1; l < 5; l++)
                    {
                        drawlife(n > 0 ? 3 : 2, 244 - l * 20, 0);
                        n--;
                    }
                }
            }
        }
    }
}