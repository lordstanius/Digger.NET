/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port by Mladen Stanisic

using SDL2;

namespace Digger.Net
{
    public class DrawApi
    {
        private const int DIR_NONE = Const.DIR_NONE;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;
        private const int BONUSES = Const.BONUSES;
        private const int BAGS = Const.BAGS;
        private const int FIREBALLS = Const.FIREBALLS;
        private const int DIGGERS = Const.DIGGERS;
        private const int MAX_TEXT_LEN = Const.MAX_TEXT_LEN;
        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int MSIZE = Const.MSIZE;
        private const int MAX_W = Const.MAX_W;
        private const int MAX_H = Const.MAX_H;
        private const int CHR_W = Const.CHR_W;
        private const int CHR_H = Const.CHR_H;

        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int MONSTERS = Const.MONSTERS;
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;
        private const int FIRSTBAG = Const.FIRSTBAG;

        public const int FIRSTBONUS = Const.FIRSTBONUS;
        public const int LASTBONUS = Const.LASTBONUS;
        public const int LASTBAG = Const.LASTBAG;
        public const int LASTMONSTER = Const.LASTMONSTER;
        public const int FIRSTFIREBALL = Const.FIRSTFIREBALL;
        public const int LASTFIREBALL = Const.LASTFIREBALL;
        public const int LASTDIGGER = Const.LASTDIGGER;

        public int[] field1 = new int[MSIZE];
        public int[] field2 = new int[MSIZE];
        public int[] field = new int[MSIZE];

        public Surface[] monbufs = new Surface[MONSTERS];
        public Surface[] bagbufs = new Surface[BAGS];
        public Surface[] bonusbufs = new Surface[BONUSES];
        public Surface[] diggerbufs = new Surface[DIGGERS];
        public Surface[] firebufs = new Surface[FIREBALLS];

        public ushort[] bitmasks = { 0xfffe, 0xfffd, 0xfffb, 0xfff7, 0xffef, 0xffdf, 0xffbf, 0xff7f, 0xfeff, 0xfdff, 0xfbff, 0xf7ff };

        public int[] digspr = new int[DIGGERS];
        public int[] digspd = new int[DIGGERS];
        public int[] firespr = new int[FIREBALLS];

        private readonly Game game;
        private readonly Sprites sprite;
        public readonly SdlGraphics gfx;

        public DrawApi(Game game)
        {
            this.game = game;
            this.sprite = game.sprites;
            this.gfx = game.gfx;
        }

        public readonly string empty_line = new string(' ', MAX_TEXT_LEN + 1);

        private void TextOut(string text, int x, int y, int c, int l)
        {
#if DEBUG
  System.Diagnostics.Debug.Assert(l > 0 && l <= MAX_TEXT_LEN);
#endif
            for (int i = 0; i < l; i++)
            {
                gfx.WriteChar(x, y, Alpha.isvalchar(text[i]) ? text[i] : ' ', c);
                x += CHR_W;
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
            for (int x = 0; x < MWIDTH; x++)
            {
                for (int y = 0; y < MHEIGHT; y++)
                {
                    field[y * MWIDTH + x] = -1;
                    char c = level.GetLevelChar(x, y, level.LevelPlan());
                    if (c == 'S' || c == 'V')
                        field[y * MWIDTH + x] &= 0xd03f;
                    if (c == 'S' || c == 'H')
                        field[y * MWIDTH + x] &= 0xdfe0;
                    if (game.CurrentPlayer == 0)
                        field1[y * MWIDTH + x] = field[y * MWIDTH + x];
                    else
                        field2[y * MWIDTH + x] = field[y * MWIDTH + x];
                }
            }
        }

        public void DrawStatistics(Level level)
        {
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (game.CurrentPlayer == 0)
                        field[y * MWIDTH + x] = field1[y * MWIDTH + x];
                    else
                        field[y * MWIDTH + x] = field2[y * MWIDTH + x];
            gfx.SetPalette(0);
            gfx.SetIntensity(0);
            DrawBackground(level.LevelPlan());
            DrawField();
        }

        public void SaveField()
        {
            int x, y;
            for (x = 0; x < MWIDTH; x++)
                for (y = 0; y < MHEIGHT; y++)
                    if (game.CurrentPlayer == 0)
                        field1[y * MWIDTH + x] = field[y * MWIDTH + x];
                    else
                        field2[y * MWIDTH + x] = field[y * MWIDTH + x];
        }

        public void DrawField()
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
                            DrawBottomBlob(xp, yp - 15);
                            DrawBottomBlob(xp, yp - 12);
                            DrawBottomBlob(xp, yp - 9);
                            DrawBottomBlob(xp, yp - 6);
                            DrawBottomBlob(xp, yp - 3);
                            DrawTopBlob(xp, yp + 3);
                        }
                        if ((field[y * MWIDTH + x] & 0x1f) != 0x1f)
                        {
                            field[y * MWIDTH + x] &= 0xdfe0;
                            DrawRightBlob(xp - 16, yp);
                            DrawRightBlob(xp - 12, yp);
                            DrawRightBlob(xp - 8, yp);
                            DrawRightBlob(xp - 4, yp);
                            DrawLeftBlob(xp + 4, yp);
                        }
                        if (x < 14)
                            if ((field[y * MWIDTH + x + 1] & 0xfdf) != 0xfdf)
                                DrawRightBlob(xp, yp);
                        if (y < 9)
                            if ((field[(y + 1) * MWIDTH + x] & 0xfdf) != 0xfdf)
                                DrawBottomBlob(xp, yp);
                    }
        }

        public void EatField(int x, int y, int dir)
        {
            int h = (x - 12) / 20, xr = ((x - 12) % 20) / 4, v = (y - 18) / 18, yr = ((y - 18) % 18) / 3;
            game.IncreasePenalty();
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

        public void CreateMBSprite()
        {
            for (int i = 0; i < BAGS; i++)
                sprite.CreateSprite(FIRSTBAG + i, 62, bagbufs[i], 4, 15, 0, 0);

            for (int i = 0; i < MONSTERS; i++)
                sprite.CreateSprite(FIRSTMONSTER + i, 71, monbufs[i], 4, 15, 0, 0);

            CreateDBFSprite();
        }

        public void InitializeMBSprite()
        {
            for (int i = 0; i < BAGS; i++)
                sprite.InitializeSprite(FIRSTBAG + i, 62, 4, 15, 0, 0);

            for (int i = 0; i < MONSTERS; i++)
                sprite.InitializeSprite(FIRSTMONSTER + i, 71, 4, 15, 0, 0);

            InitDBFSprite();
        }

        public void DrawGold(int n, int t, int x, int y)
        {
            sprite.InitializeSprite(FIRSTBAG + n, t + 62, 4, 15, 0, 0);
            sprite.DrawSprite(FIRSTBAG + n, x, y);
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

        public void CreateDBFSprite()
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
                sprite.CreateSprite(i, 0, diggerbufs[i - FIRSTDIGGER], 4, 15, 0, 0);
            for (i = FIRSTBONUS; i < LASTBONUS; i++)
                sprite.CreateSprite(i, 81, bonusbufs[i - FIRSTBONUS], 4, 15, 0, 0);
            for (i = FIRSTFIREBALL; i < LASTFIREBALL; i++)
                sprite.CreateSprite(i, 82, firebufs[i - FIRSTFIREBALL], 2, 8, 0, 0);
        }

        public void InitDBFSprite()
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
                sprite.InitializeSprite(i, 0, 4, 15, 0, 0);
            for (i = FIRSTBONUS; i < LASTBONUS; i++)
                sprite.InitializeSprite(i, 81, 4, 15, 0, 0);
            for (i = FIRSTFIREBALL; i < LASTFIREBALL; i++)
                sprite.InitializeSprite(i, 82, 2, 8, 0, 0);
        }

        public void DrawRightBlob(int x, int y)
        {
            sprite.initmiscspr(x + 16, y - 1, 2, 18);
            sprite.drawmiscspr(x + 16, y - 1, 102, 2, 18);
            sprite.getis();
        }

        public void DrawLeftBlob(int x, int y)
        {
            sprite.initmiscspr(x - 8, y - 1, 2, 18);
            sprite.drawmiscspr(x - 8, y - 1, 104, 2, 18);
            sprite.getis();
        }

        public void DrawTopBlob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y - 6, 6, 6);
            sprite.drawmiscspr(x - 4, y - 6, 103, 6, 6);
            sprite.getis();
        }

        public void DrawBottomBlob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y + 15, 6, 6);
            sprite.drawmiscspr(x - 4, y + 15, 105, 6, 6);
            sprite.getis();
        }

        public void DrawFurryBlob(int x, int y)
        {
            sprite.initmiscspr(x - 4, y + 15, 6, 8);
            sprite.drawmiscspr(x - 4, y + 15, 107, 6, 8);
            sprite.getis();
        }

        public void DrawSquareBlob(int x, int y)
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

        public void DrawFire(int n, int x, int y, int t)
        {
            int nn = (n == 0) ? 0 : 32;
            if (t == 0)
            {
                firespr[n]++;
                if (firespr[n] > 2)
                    firespr[n] = 0;
                sprite.InitializeSprite(FIRSTFIREBALL + n, 82 + firespr[n] + nn, 2, 8, 0, 0);
            }
            else
                sprite.InitializeSprite(FIRSTFIREBALL + n, 84 + t + nn, 2, 8, 0, 0);
            sprite.DrawSprite(FIRSTFIREBALL + n, x, y);
        }

        public void DrawBonus(int x, int y)
        {
            int n = 0;
            sprite.InitializeSprite(FIRSTBONUS + n, 81, 4, 15, 0, 0);
            sprite.MoveDrawSprite(FIRSTBONUS + n, x, y);
        }

        public void DrawDigger(int n, int t, int x, int y, bool f)
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
                sprite.InitializeSprite(FIRSTDIGGER + n, (t + (f ? 0 : 1)) * 3 + digspr[n] + 1 + nn, 4, 15, 0, 0);
                sprite.DrawSprite(FIRSTDIGGER + n, x, y);
                return;
            }
            if (t >= 10 && t <= 15)
            {
                sprite.InitializeSprite(FIRSTDIGGER + n, 40 + nn - t, 4, 15, 0, 0);
                sprite.DrawSprite(FIRSTDIGGER + n, x, y);
                return;
            }
            sprite.first[0] = sprite.first[1] = sprite.first[2] = sprite.first[3] = sprite.first[4] = -1;
        }
    }
}