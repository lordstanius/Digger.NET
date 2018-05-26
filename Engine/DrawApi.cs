/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using SDL2;

namespace Digger.Net
{
    public class DrawApi
    {
        public int[] field1 = new int[DiggerC.MSIZE];
        public int[] field2 = new int[DiggerC.MSIZE];
        public int[] field = new int[DiggerC.MSIZE];

        public Surface[] monbufs = new Surface[DiggerC.MONSTERS];
        public Surface[] bagbufs = new Surface[DiggerC.BAGS];
        public Surface[] bonusbufs = new Surface[DiggerC.BONUSES];
        public Surface[] diggerbufs = new Surface[DiggerC.DIGGERS];
        public Surface[] firebufs = new Surface[DiggerC.FIREBALLS];

        public ushort[] bitmasks = { 0xfffe, 0xfffd, 0xfffb, 0xfff7, 0xffef, 0xffdf, 0xffbf, 0xff7f, 0xfeff, 0xfdff, 0xfbff, 0xf7ff };

        public int[] digspr = new int[DiggerC.DIGGERS];
        public int[] digspd = new int[DiggerC.DIGGERS];
        public int[] firespr = new int[DiggerC.FIREBALLS];

        private readonly Sprites sprite;
        public readonly SdlGraphics gfx;

        public DrawApi(SdlGraphics gfx, Sprites sprite)
        {
            this.sprite = sprite;
            this.gfx = gfx;
        }

        public readonly string empty_line = new string(' ', DiggerC.MAX_TEXT_LEN + 1);

        private void TextOut(string text, int x, int y, int c, int l)
        {
#if DEBUG
  System.Diagnostics.Debug.Assert(l > 0 && l <= DiggerC.MAX_TEXT_LEN);
#endif
            for (int i = 0; i < l; i++)
            {
                gfx.WriteChar(x, y, Alpha.isvalchar(text[i]) ? text[i] : ' ', c);
                x += DiggerC.CHR_W;
            }
        }

        public void TextOut(string text, int x, int y, int c)
        {
            TextOut(text, x, y, c, text.Length);
        }

        public void EraseText(short n, int x, int y, short c)
        {
            TextOut(empty_line, x, y, c, n);
        }

        public void MakeField(Level level)
        {
            for (int x = 0; x < DiggerC.MWIDTH; x++)
            {
                for (int y = 0; y < DiggerC.MHEIGHT; y++)
                {
                    field[y * DiggerC.MWIDTH + x] = -1;
                    char c = level.getlevch(x, y, level.levplan());
                    if (c == 'S' || c == 'V')
                        field[y * DiggerC.MWIDTH + x] &= 0xd03f;
                    if (c == 'S' || c == 'H')
                        field[y * DiggerC.MWIDTH + x] &= 0xdfe0;
                    if (DiggerC.g_CurrentPlayer == 0)
                        field1[y * DiggerC.MWIDTH + x] = field[y * DiggerC.MWIDTH + x];
                    else
                        field2[y * DiggerC.MWIDTH + x] = field[y * DiggerC.MWIDTH + x];
                }
            }
        }

        public void DrawStatistics(SdlGraphics sdlGfx, Level level)
        {
            for (int x = 0; x < DiggerC.MWIDTH; x++)
                for (int y = 0; y < DiggerC.MHEIGHT; y++)
                    if (DiggerC.g_CurrentPlayer == 0)
                        field[y * DiggerC.MWIDTH + x] = field1[y * DiggerC.MWIDTH + x];
                    else
                        field[y * DiggerC.MWIDTH + x] = field2[y * DiggerC.MWIDTH + x];
            sdlGfx.SetPalette(0);
            sdlGfx.SetIntensity(0);
            DrawBackground(level.levplan());
            DrawField();
        }

        public void SaveField()
        {
            int x, y;
            for (x = 0; x < DiggerC.MWIDTH; x++)
                for (y = 0; y < DiggerC.MHEIGHT; y++)
                    if (DiggerC.g_CurrentPlayer == 0)
                        field1[y * DiggerC.MWIDTH + x] = field[y * DiggerC.MWIDTH + x];
                    else
                        field2[y * DiggerC.MWIDTH + x] = field[y * DiggerC.MWIDTH + x];
        }

        public void DrawField()
        {
            int x, y, xp, yp;
            for (x = 0; x < DiggerC.MWIDTH; x++)
                for (y = 0; y < DiggerC.MHEIGHT; y++)
                    if ((field[y * DiggerC.MWIDTH + x] & 0x2000) == 0)
                    {
                        xp = x * 20 + 12;
                        yp = y * 18 + 18;
                        if ((field[y * DiggerC.MWIDTH + x] & 0xfc0) != 0xfc0)
                        {
                            field[y * DiggerC.MWIDTH + x] &= 0xd03f;
                            drawbottomblob(xp, yp - 15);
                            drawbottomblob(xp, yp - 12);
                            drawbottomblob(xp, yp - 9);
                            drawbottomblob(xp, yp - 6);
                            drawbottomblob(xp, yp - 3);
                            drawtopblob(xp, yp + 3);
                        }
                        if ((field[y * DiggerC.MWIDTH + x] & 0x1f) != 0x1f)
                        {
                            field[y * DiggerC.MWIDTH + x] &= 0xdfe0;
                            drawrightblob(xp - 16, yp);
                            drawrightblob(xp - 12, yp);
                            drawrightblob(xp - 8, yp);
                            drawrightblob(xp - 4, yp);
                            drawleftblob(xp + 4, yp);
                        }
                        if (x < 14)
                            if ((field[y * DiggerC.MWIDTH + x + 1] & 0xfdf) != 0xfdf)
                                drawrightblob(xp, yp);
                        if (y < 9)
                            if ((field[(y + 1) * DiggerC.MWIDTH + x] & 0xfdf) != 0xfdf)
                                drawbottomblob(xp, yp);
                    }
        }

        public void EatField(int x, int y, int dir)
        {
            int h = (x - 12) / 20, xr = ((x - 12) % 20) / 4, v = (y - 18) / 18, yr = ((y - 18) % 18) / 3;
            DiggerC.incpenalty();
            switch (dir)
            {
                case DiggerC.DIR_RIGHT:
                    h++;
                    field[v * DiggerC.MWIDTH + h] &= bitmasks[xr];
                    if ((field[v * DiggerC.MWIDTH + h] & 0x1f) != 0)
                        break;
                    field[v * DiggerC.MWIDTH + h] &= 0xdfff;
                    break;
                case DiggerC.DIR_UP:
                    yr--;
                    if (yr < 0)
                    {
                        yr += 6;
                        v--;
                    }
                    field[v * DiggerC.MWIDTH + h] &= bitmasks[6 + yr];
                    if ((field[v * DiggerC.MWIDTH + h] & 0xfc0) != 0)
                        break;
                    field[v * DiggerC.MWIDTH + h] &= 0xdfff;
                    break;
                case DiggerC.DIR_LEFT:
                    xr--;
                    if (xr < 0)
                    {
                        xr += 5;
                        h--;
                    }
                    field[v * DiggerC.MWIDTH + h] &= bitmasks[xr];
                    if ((field[v * DiggerC.MWIDTH + h] & 0x1f) != 0)
                        break;
                    field[v * DiggerC.MWIDTH + h] &= 0xdfff;
                    break;
                case DiggerC.DIR_DOWN:
                    v++;
                    field[v * DiggerC.MWIDTH + h] &= bitmasks[6 + yr];
                    if ((field[v * DiggerC.MWIDTH + h] & 0xfc0) != 0)
                        break;
                    field[v * DiggerC.MWIDTH + h] &= 0xdfff;
                    break;
            }
        }

        public void creatembspr()
        {
            for (int i = 0; i < DiggerC.BAGS; i++)
                sprite.CreateSprite(DiggerC.FIRSTBAG + i, 62, bagbufs[i], 4, 15, 0, 0);
            for (int i = 0; i < DiggerC.MONSTERS; i++)
                sprite.CreateSprite(DiggerC.FIRSTMONSTER + i, 71, monbufs[i], 4, 15, 0, 0);
            CreateSpriteDb();
        }

        public void initmbspr()
        {
            for (int i = 0; i < DiggerC.BAGS; i++)
                sprite.InitializeSprite(DiggerC.FIRSTBAG + i, 62, 4, 15, 0, 0);

            for (int i = 0; i < DiggerC.MONSTERS; i++)
                sprite.InitializeSprite(DiggerC.FIRSTMONSTER + i, 71, 4, 15, 0, 0);

            initdbfspr();
        }

        public void DrawGold(int n, int t, int x, int y)
        {
            sprite.InitializeSprite(DiggerC.FIRSTBAG + n, t + 62, 4, 15, 0, 0);
            sprite.DrawSprite(DiggerC.FIRSTBAG + n, x, y);
        }

        public void DrawLife(int t, int x, int y)
        {
            sprite.drawmiscspr(x, y, t + 110, 4, 12);
        }

        public void DrawEmerald(int x, int y)
        {
            sprite.initmiscspr(x, y, 4, 10);
            sprite.drawmiscspr(x, y, 108, 4, 10);
            sprite.getis();
        }

        public void EraseEmerald(int x, int y)
        {
            sprite.initmiscspr(x, y, 4, 10);
            sprite.drawmiscspr(x, y, 109, 4, 10);
            sprite.getis();
        }

        public void CreateSpriteDb()
        {
            int i;
            for (i = 0; i < DiggerC.DIGGERS; i++)
            {
                digspd[i] = 1;
                digspr[i] = 0;
            }
            for (i = 0; i < DiggerC.FIREBALLS; i++)
                firespr[i] = 0;
            for (i = DiggerC.FIRSTDIGGER; i < DiggerC.LASTDIGGER; i++)
                sprite.CreateSprite(i, 0, diggerbufs[i - DiggerC.FIRSTDIGGER], 4, 15, 0, 0);
            for (i = DiggerC.FIRSTBONUS; i < DiggerC.LASTBONUS; i++)
                sprite.CreateSprite(i, 81, bonusbufs[i - DiggerC.FIRSTBONUS], 4, 15, 0, 0);
            for (i = DiggerC.FIRSTFIREBALL; i < DiggerC.LASTFIREBALL; i++)
                sprite.CreateSprite(i, 82, firebufs[i - DiggerC.FIRSTFIREBALL], 2, 8, 0, 0);
        }

        public void initdbfspr()
        {
            int i;
            for (i = 0; i < DiggerC.DIGGERS; i++)
            {
                digspd[i] = 1;
                digspr[i] = 0;
            }
            for (i = 0; i < DiggerC.FIREBALLS; i++)
                firespr[i] = 0;
            for (i = DiggerC.FIRSTDIGGER; i < DiggerC.LASTDIGGER; i++)
                sprite.InitializeSprite(i, 0, 4, 15, 0, 0);
            for (i = DiggerC.FIRSTBONUS; i < DiggerC.LASTBONUS; i++)
                sprite.InitializeSprite(i, 81, 4, 15, 0, 0);
            for (i = DiggerC.FIRSTFIREBALL; i < DiggerC.LASTFIREBALL; i++)
                sprite.InitializeSprite(i, 82, 2, 8, 0, 0);
        }

        public void drawrightblob(int x, int y)
        {
            sprite.initmiscspr(x + 16, y - 1, 2, 18);
            sprite.drawmiscspr(x + 16, y - 1, 102, 2, 18);
            sprite.getis();
        }

        public void drawleftblob(int x, int y)
        {
            sprite.initmiscspr(x - 8, y - 1, 2, 18);
            sprite.drawmiscspr(x - 8, y - 1, 104, 2, 18);
            sprite.getis();
        }

        public void drawtopblob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y - 6, 6, 6);
            sprite.drawmiscspr(x - 4, y - 6, 103, 6, 6);
            sprite.getis();
        }

        public void drawbottomblob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y + 15, 6, 6);
            sprite.drawmiscspr(x - 4, y + 15, 105, 6, 6);
            sprite.getis();
        }

        public void drawfurryblob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y + 15, 6, 8);
            sprite.drawmiscspr(x - 4, y + 15, 107, 6, 8);
            sprite.getis();
        }

        public void drawsquareblob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y + 17, 6, 6);
            sprite.drawmiscspr(x - 4, y + 17, 106, 6, 6);
            sprite.getis();
        }

        public void DrawBackground(int l)
        {
            for (int y = 14; y < 200; y += 4)
            {
                for (int x = 0; x < 320; x += 20)
                    sprite.drawmiscspr(x, y, 93 + l, 5, 4);
            }
        }

        public void drawfire(int n, int x, int y, int t)
        {
            int nn = (n == 0) ? 0 : 32;
            if (t == 0)
            {
                firespr[n]++;
                if (firespr[n] > 2)
                    firespr[n] = 0;
                sprite.InitializeSprite(DiggerC.FIRSTFIREBALL + n, 82 + firespr[n] + nn, 2, 8, 0, 0);
            }
            else
                sprite.InitializeSprite(DiggerC.FIRSTFIREBALL + n, 84 + t + nn, 2, 8, 0, 0);
            sprite.DrawSprite(DiggerC.FIRSTFIREBALL + n, x, y);
        }

        public void drawbonus(int x, int y)
        {
            int n = 0;
            sprite.InitializeSprite(DiggerC.FIRSTBONUS + n, 81, 4, 15, 0, 0);
            sprite.movedrawspr(DiggerC.FIRSTBONUS + n, x, y);
        }

        public void drawdigger(int n, int t, int x, int y, bool f)
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
                sprite.InitializeSprite(DiggerC.FIRSTDIGGER + n, (t + (f ? 0 : 1)) * 3 + digspr[n] + 1 + nn, 4, 15, 0, 0);
                sprite.DrawSprite(DiggerC.FIRSTDIGGER + n, x, y);
                return;
            }
            if (t >= 10 && t <= 15)
            {
                sprite.InitializeSprite(DiggerC.FIRSTDIGGER + n, 40 + nn - t, 4, 15, 0, 0);
                sprite.DrawSprite(DiggerC.FIRSTDIGGER + n, x, y);
                return;
            }
            sprite.first[0] = sprite.first[1] = sprite.first[2] = sprite.first[3] = sprite.first[4] = -1;
        }
    }
}