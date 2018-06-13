/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using SDL2;

namespace Digger.Source
{
    public class Sprites
    {
        public const int SPRITES = Const.SPRITES;

        public int[] first = new int[Const.TYPES];
        public int[] coll = new int[SPRITES];

        private int[] firstt = { Const.FIRSTBONUS, Const.FIRSTBAG, Const.FIRSTMONSTER, Const.FIRSTFIREBALL, Const.FIRSTDIGGER };
        private int[] lastt = { Const.LASTBONUS, Const.LASTBAG, Const.LASTMONSTER, Const.LASTFIREBALL, Const.LASTDIGGER };

        private readonly bool[] spriteNeedsRedrawFlag = new bool[SPRITES + 1];
        private readonly bool[] spriteRecursionFlag = new bool[SPRITES + 1];
        private readonly bool[] spriteEnabledFlag = new bool[SPRITES];
        private readonly int[] spriteChar = new int[SPRITES + 1];
        private readonly int[] spriteX = new int[SPRITES + 1];
        private readonly int[] spriteY = new int[SPRITES + 1];
        private readonly int[] spriteWidth = new int[SPRITES + 1];
        private readonly int[] spriteHeigth = new int[SPRITES + 1];
        private readonly int[] spriteBorderWidth = new int[SPRITES];
        private readonly int[] spriteBorderHeigth = new int[SPRITES];
        private readonly int[] newSpriteChar = new int[SPRITES];
        private readonly int[] newSpriteWidth = new int[SPRITES];
        private readonly int[] newSpriteHeigth = new int[SPRITES];
        private readonly int[] newSpriteBorderWidth = new int[SPRITES];
        private readonly int[] newSpriteBorderHeight = new int[SPRITES];
        private readonly Surface[] spriteBuffer = new Surface[SPRITES];

        private Game game;

        public Sprites(Game game)
        {
            this.game = game;
        }

        public void CreateSprite(int n, int ch, Surface mov, int wid, int hei, int bwid, int bhei)
        {
            newSpriteChar[n] = spriteChar[n] = ch;
            spriteBuffer[n] = mov;
            newSpriteWidth[n] = spriteWidth[n] = wid;
            newSpriteHeigth[n] = spriteHeigth[n] = hei;
            newSpriteBorderWidth[n] = spriteBorderWidth[n] = bwid;
            newSpriteBorderHeight[n] = spriteBorderHeigth[n] = bhei;
            spriteEnabledFlag[n] = false;
        }

        public void MoveDrawSprite(int n, int x, int y)
        {
            spriteX[n] = (short)(x & -4);
            spriteY[n] = y;
            spriteChar[n] = newSpriteChar[n];
            spriteWidth[n] = newSpriteWidth[n];
            spriteHeigth[n] = newSpriteHeigth[n];
            spriteBorderWidth[n] = newSpriteBorderWidth[n];
            spriteBorderHeigth[n] = newSpriteBorderHeight[n];
            ClearRedrawFlag();
            SetRedrawFlags(n);
            RedrawBackgroudImages();
            game.gfx.GetImage(spriteX[n], spriteY[n], ref spriteBuffer[n], spriteWidth[n], spriteHeigth[n]);
            spriteEnabledFlag[n] = true;
            spriteNeedsRedrawFlag[n] = true;
            DrawActualSprites();
        }

        public void EraseSprite(int n)
        {
            if (!spriteEnabledFlag[n])
                return;
            game.gfx.PutImage(spriteX[n], spriteY[n], spriteBuffer[n], spriteWidth[n], spriteHeigth[n]);
            spriteEnabledFlag[n] = false;
            ClearRedrawFlag();
            SetRedrawFlags(n);
            DrawActualSprites();
        }

        public void DrawSprite(int n, int x, int y)
        {
            x &= -4;
            ClearRedrawFlag();
            SetRedrawFlags(n);
            int t1 = spriteX[n];
            int t2 = spriteY[n];
            int t3 = spriteWidth[n];
            int t4 = spriteHeigth[n];
            spriteX[n] = x;
            spriteY[n] = y;
            spriteWidth[n] = newSpriteWidth[n];
            spriteHeigth[n] = newSpriteHeigth[n];
            ClearRecursionFlags();
            SetRedrawFlags(n);
            spriteHeigth[n] = t4;
            spriteWidth[n] = t3;
            spriteY[n] = t2;
            spriteX[n] = t1;
            spriteNeedsRedrawFlag[n] = true;
            RedrawBackgroudImages();
            spriteEnabledFlag[n] = true;
            spriteX[n] = x;
            spriteY[n] = y;
            spriteChar[n] = newSpriteChar[n];
            spriteWidth[n] = newSpriteWidth[n];
            spriteHeigth[n] = newSpriteHeigth[n];
            spriteBorderWidth[n] = newSpriteBorderWidth[n];
            spriteBorderHeigth[n] = newSpriteBorderHeight[n];
            game.gfx.GetImage(spriteX[n], spriteY[n], ref spriteBuffer[n], spriteWidth[n], spriteHeigth[n]);

            DrawActualSprites();
            CreateCollisionLinkedList(n);
        }

        public void InitializeSprite(int n, int ch, int wid, int hei, short bwid, short bhei)
        {
            newSpriteChar[n] = ch;
            newSpriteWidth[n] = wid;
            newSpriteHeigth[n] = hei;
            newSpriteBorderWidth[n] = bwid;
            newSpriteBorderHeight[n] = bhei;
        }

        public void InitializeMiscSprites(int x, int y, int wid, int hei)
        {
            spriteX[SPRITES] = x;
            spriteY[SPRITES] = y;
            spriteWidth[SPRITES] = wid;
            spriteHeigth[SPRITES] = hei;
            ClearRedrawFlag();
            SetRedrawFlags(SPRITES);
            RedrawBackgroudImages();
        }

        public void getis()
        {
            for (int i = 0; i < SPRITES; i++)
                if (spriteNeedsRedrawFlag[i])
                    game.gfx.GetImage(spriteX[i], spriteY[i], ref spriteBuffer[i], spriteWidth[i], spriteHeigth[i]);
            DrawActualSprites();
        }

        public void DrawMiscSprite(int x, int y, int ch, short wid, short hei)
        {
            spriteX[SPRITES] = x & -4;
            spriteY[SPRITES] = y;
            spriteChar[SPRITES] = ch;
            spriteWidth[SPRITES] = wid;
            spriteHeigth[SPRITES] = hei;
            game.gfx.PutImage(spriteX[SPRITES], spriteY[SPRITES], spriteChar[SPRITES], spriteWidth[SPRITES], spriteHeigth[SPRITES]);
        }

        public void ClearRedrawFlag()
        {
            ClearRecursionFlags();
            for (int i = 0; i < SPRITES + 1; i++)
                spriteNeedsRedrawFlag[i] = false;
        }

        public void ClearRecursionFlags()
        {
            for (int i = 0; i < SPRITES + 1; i++)
                spriteRecursionFlag[i] = false;
        }

        public void SetRedrawFlags(int n)
        {
            if (!spriteRecursionFlag[n])
            {
                spriteRecursionFlag[n] = true;
                for (int i = 0; i < SPRITES; i++)
                    if (spriteEnabledFlag[i] && i != n)
                    {
                        if (CheckCollision(i, n))
                        {
                            spriteNeedsRedrawFlag[i] = true;
                            SetRedrawFlags(i);
                        }
                    }
            }
        }

        public bool CheckCollision(int bx, int si)
        {
            if (spriteX[bx] >= spriteX[si])
            {
                if (spriteX[bx] > (spriteWidth[si] << 2) + spriteX[si] - 1)
                    return false;
            }
            else
              if (spriteX[si] > (spriteWidth[bx] << 2) + spriteX[bx] - 1)
                return false;
            if (spriteY[bx] >= spriteY[si])
            {
                if (spriteY[bx] <= spriteHeigth[si] + spriteY[si] - 1)
                    return true;
                return false;
            }
            if (spriteY[si] <= spriteHeigth[bx] + spriteY[bx] - 1)
                return true;
            return false;
        }

        public bool CheckBorderCollision(int bx, int si)
        {
            if (spriteX[bx] >= spriteX[si])
            {
                if (spriteX[bx] + spriteBorderWidth[bx] > (spriteWidth[si] << 2) + spriteX[si] - spriteBorderWidth[si] - 1)
                    return false;
            }
            else
            {
                if (spriteX[si] + spriteBorderWidth[si] > (spriteWidth[bx] << 2) + spriteX[bx] - spriteBorderWidth[bx] - 1)
                    return false;
            }

            if (spriteY[bx] >= spriteY[si])
            {
                if (spriteY[bx] + spriteBorderHeigth[bx] <= spriteHeigth[si] + spriteY[si] - spriteBorderHeigth[si] - 1)
                    return true;
                return false;
            }

            return spriteY[si] + spriteBorderHeigth[si] <= spriteHeigth[bx] + spriteY[bx] - spriteBorderHeigth[bx] - 1;
        }

        public void DrawActualSprites()
        {
            for (int i = 0; i < SPRITES; i++)
                if (spriteNeedsRedrawFlag[i])
                    game.gfx.PutImage(spriteX[i], spriteY[i], spriteChar[i], spriteWidth[i], spriteHeigth[i]);
        }

        public void RedrawBackgroudImages()
        {
            for (int i = 0; i < SPRITES; i++)
                if (spriteNeedsRedrawFlag[i])
                    game.gfx.PutImage(spriteX[i], spriteY[i], spriteBuffer[i], spriteWidth[i], spriteHeigth[i]);
        }

        public void CreateCollisionLinkedList(int spr)
        {
            for (int next = 0; next < Const.TYPES; next++)
                first[next] = -1;

            for (int next = 0; next < SPRITES; next++)
                coll[next] = -1;

            for (int i = 0; i < Const.TYPES; i++)
            {
                int next = -1;
                for (int spc = firstt[i]; spc < lastt[i]; spc++)
                {
                    if (spriteEnabledFlag[spc] && spc != spr)
                    {
                        if (CheckBorderCollision(spr, spc))
                        {
                            if (next == -1)
                                first[i] = next = spc;
                            else
                                coll[next = (coll[next] = spc)] = -1;
                        }
                    }
                }
            }
        }

        private void WriteDebug(int x, int y, char ch, short c)
        {
            System.Diagnostics.Debug.Assert(x + Const.CHR_W <= Const.MAX_W);
            System.Diagnostics.Debug.Assert(y + Const.CHR_H <= Const.MAX_H);
            game.gfx.WriteChar(x, y, ch, c);
        }
    }
}