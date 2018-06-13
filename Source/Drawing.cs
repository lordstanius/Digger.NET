/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using SDL2;

namespace Digger.Source
{
    public class Drawing
    {
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

        public readonly string EmptyLine = new string(' ', MAX_TEXT_LEN);

        private Game game;

        public Drawing(Game game)
        {
            this.game = game;
        }

        private void TextOut(string text, int x, int y, int c, int l)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(l > 0 && l <= MAX_TEXT_LEN);
#endif
            for (int i = 0; i < l; i++)
            {
                game.gfx.WriteChar(x, y, Alpha.IsValidChar(text[i]) ? text[i] : ' ', c);
                x += CHR_W;
            }
        }

        public void TextOut(string text, int x, int y, int c)
        {
            TextOut(text, x, y, c, text.Length);
        }

        public void TextOutCentered(string text, int y, int c)
        {
            int x = (MAX_TEXT_LEN - text.Length) / 2 * CHR_W;
            TextOut(text, x, y, c);
        }

        public void EraseText(short n, int x, int y, short c)
        {
            TextOut(EmptyLine, x, y, c, n);
        }

        public void EraseLine(int y)
        {
            EraseText(MAX_TEXT_LEN, 0, y, 0);
        }

        public void MakeField()
        {
            for (int x = 0; x < MWIDTH; x++)
            {
                for (int y = 0; y < MHEIGHT; y++)
                {
                    field[y * MWIDTH + x] = -1;
                    char c = Level.GetLevelChar(x, y, game.LevelNo, game.diggerCount);
                    if (c == 'S' || c == 'V')
                        field[y * MWIDTH + x] &= 0xd03f;
                    if (c == 'S' || c == 'H')
                        field[y * MWIDTH + x] &= 0xdfe0;
                    if (game.currentPlayer == 0)
                        field1[y * MWIDTH + x] = field[y * MWIDTH + x];
                    else
                        field2[y * MWIDTH + x] = field[y * MWIDTH + x];
                }
            }
        }

        public void DrawStatistics()
        {
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (game.currentPlayer == 0)
                        field[y * MWIDTH + x] = field1[y * MWIDTH + x];
                    else
                        field[y * MWIDTH + x] = field2[y * MWIDTH + x];
            game.gfx.SetIntensity(0);
            DrawBackground(Level.LevelPlan(game.LevelNo));
            DrawField();
        }

        public void SaveField()
        {
            int x, y;
            for (x = 0; x < MWIDTH; x++)
                for (y = 0; y < MHEIGHT; y++)
                    if (game.currentPlayer == 0)
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
            int h = (x - 12) / 20;
            int xr = ((x - 12) % 20) / 4;
            int v = (y - 18) / 18;
            int yr = ((y - 18) % 18) / 3;

            game.IncreasePenalty();

            switch (dir)
            {
                case Dir.Right:
                    h++;
                    field[v * MWIDTH + h] &= bitmasks[xr];
                    if ((field[v * MWIDTH + h] & 0x1f) != 0)
                        break;
                    field[v * MWIDTH + h] &= 0xdfff;
                    break;
                case Dir.Up:
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
                case Dir.Left:
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
                case Dir.Down:
                    v++;
                    field[v * MWIDTH + h] &= bitmasks[6 + yr];
                    if ((field[v * MWIDTH + h] & 0xfc0) != 0)
                        break;
                    field[v * MWIDTH + h] &= 0xdfff;
                    break;
            }
        }

        public void FlashyWait(int n)
        {
            int p = 0;
            for (int i = 0; i < n; i++)
            {
                p = 1 - p;
                game.gfx.SetIntensity(p);
                game.timer.SyncFrame();
                game.gfx.UpdateScreen();
            }
        }

        public void CreateMonsterBagSprites()
        {
            for (int i = 0; i < BAGS; i++)
                game.sprites.CreateSprite(FIRSTBAG + i, 62, bagbufs[i], 4, 15, 0, 0);

            for (int i = 0; i < MONSTERS; i++)
                game.sprites.CreateSprite(FIRSTMONSTER + i, 71, monbufs[i], 4, 15, 0, 0);

            CreateDiggerBonusFireballSprites();
        }

        public void InitMonsterBagSprites()
        {
            for (int i = 0; i < BAGS; i++)
                game.sprites.InitializeSprite(FIRSTBAG + i, 62, 4, 15, 0, 0);

            for (int i = 0; i < MONSTERS; i++)
                game.sprites.InitializeSprite(FIRSTMONSTER + i, 71, 4, 15, 0, 0);

            InitDiggerBonusFireballSprites();
        }

        public void DrawGold(int n, int t, int x, int y)
        {
            game.sprites.InitializeSprite(FIRSTBAG + n, t + 62, 4, 15, 0, 0);
            game.sprites.DrawSprite(FIRSTBAG + n, x, y);
        }

        public void DrawLife(int t, int x, int y)
        {
            game.sprites.DrawMiscSprite(x, y, t + 110, 4, 12);
        }

        public void DrawEmerald(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x, y, 4, 10);
            game.sprites.DrawMiscSprite(x, y, 108, 4, 10);
            game.sprites.getis();
        }

        public void EraseEmerald(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x, y, 4, 10);
            game.sprites.DrawMiscSprite(x, y, 109, 4, 10);
            game.sprites.getis();
        }

        public void CreateDiggerBonusFireballSprites()
        {
            for (int i = 0; i < DIGGERS; i++)
            {
                digspd[i] = 1;
                digspr[i] = 0;
            }

            for (int i = 0; i < FIREBALLS; i++)
                firespr[i] = 0;

            for (int i = FIRSTDIGGER; i < LASTDIGGER; i++)
                game.sprites.CreateSprite(i, 0, diggerbufs[i - FIRSTDIGGER], 4, 15, 0, 0);

            for (int i = FIRSTBONUS; i < LASTBONUS; i++)
                game.sprites.CreateSprite(i, 81, bonusbufs[i - FIRSTBONUS], 4, 15, 0, 0);

            for (int i = FIRSTFIREBALL; i < LASTFIREBALL; i++)
                game.sprites.CreateSprite(i, 82, firebufs[i - FIRSTFIREBALL], 2, 8, 0, 0);
        }

        public void InitDiggerBonusFireballSprites()
        {
            for (int i = 0; i < DIGGERS; i++)
            {
                digspd[i] = 1;
                digspr[i] = 0;
            }

            for (int i = 0; i < FIREBALLS; i++)
                firespr[i] = 0;

            for (int i = FIRSTDIGGER; i < LASTDIGGER; i++)
                game.sprites.InitializeSprite(i, 0, 4, 15, 0, 0);

            for (int i = FIRSTBONUS; i < LASTBONUS; i++)
                game.sprites.InitializeSprite(i, 81, 4, 15, 0, 0);

            for (int i = FIRSTFIREBALL; i < LASTFIREBALL; i++)
                game.sprites.InitializeSprite(i, 82, 2, 8, 0, 0);
        }

        public void DrawRightBlob(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x + 16, y - 1, 2, 18);
            game.sprites.DrawMiscSprite(x + 16, y - 1, 102, 2, 18);
            game.sprites.getis();
        }

        public void DrawLeftBlob(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x - 8, y - 1, 2, 18);
            game.sprites.DrawMiscSprite(x - 8, y - 1, 104, 2, 18);
            game.sprites.getis();
        }

        public void DrawTopBlob(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x - 4, y - 6, 6, 6);
            game.sprites.DrawMiscSprite(x - 4, y - 6, 103, 6, 6);
            game.sprites.getis();
        }

        public void DrawBottomBlob(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x - 4, y + 15, 6, 6);
            game.sprites.DrawMiscSprite(x - 4, y + 15, 105, 6, 6);
            game.sprites.getis();
        }

        public void DrawFurryBlob(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x - 4, y + 15, 6, 8);
            game.sprites.DrawMiscSprite(x - 4, y + 15, 107, 6, 8);
            game.sprites.getis();
        }

        public void DrawSquareBlob(int x, int y)
        {
            game.sprites.InitializeMiscSprites(x - 4, y + 17, 6, 6);
            game.sprites.DrawMiscSprite(x - 4, y + 17, 106, 6, 6);
            game.sprites.getis();
        }

        public void DrawBackground(int l)
        {
            for (int y = 14; y < 200; y += 4)
                for (int x = 0; x < 320; x += 20)
                    game.sprites.DrawMiscSprite(x, y, 93 + l, 5, 4);
        }

        public void DrawFire(int n, int x, int y, int t)
        {
            int nn = (n == 0) ? 0 : 32;
            if (t == 0)
            {
                firespr[n]++;
                if (firespr[n] > 2)
                    firespr[n] = 0;
                game.sprites.InitializeSprite(FIRSTFIREBALL + n, 82 + firespr[n] + nn, 2, 8, 0, 0);
            }
            else
                game.sprites.InitializeSprite(FIRSTFIREBALL + n, 84 + t + nn, 2, 8, 0, 0);
            game.sprites.DrawSprite(FIRSTFIREBALL + n, x, y);
        }

        public void DrawBonus(int x, int y)
        {
            int n = 0;
            game.sprites.InitializeSprite(FIRSTBONUS + n, 81, 4, 15, 0, 0);
            game.sprites.MoveDrawSprite(FIRSTBONUS + n, x, y);
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
                game.sprites.InitializeSprite(FIRSTDIGGER + n, (t + (f ? 0 : 1)) * 3 + digspr[n] + 1 + nn, 4, 15, 0, 0);
                game.sprites.DrawSprite(FIRSTDIGGER + n, x, y);
                return;
            }

            if (t >= 10 && t <= 15)
            {
                game.sprites.InitializeSprite(FIRSTDIGGER + n, 40 + nn - t, 4, 15, 0, 0);
                game.sprites.DrawSprite(FIRSTDIGGER + n, x, y);
                return;
            }

            game.sprites.first[0] = game.sprites.first[1] = game.sprites.first[2] = game.sprites.first[3] = game.sprites.first[4] = -1;
        }
    }
}