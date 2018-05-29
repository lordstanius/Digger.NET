/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using SDL2;

namespace Digger.Net
{
    public class Sprites
    {
        public const int SPRITES = Const.SPRITES;

        public bool[] sprrdrwf = new bool[SPRITES + 1];
        public bool[] sprrecf = new bool[SPRITES + 1];
        public bool[] sprenf = new bool[SPRITES];
        public Surface[] sprmov = new Surface[SPRITES];
        public int[] sprch = new int[SPRITES + 1];
        public int[] sprx = new int[SPRITES + 1];
        public int[] spry = new int[SPRITES + 1];
        public int[] sprwid = new int[SPRITES + 1];
        public int[] sprhei = new int[SPRITES + 1];
        public int[] sprbwid = new int[SPRITES];
        public int[] sprbhei = new int[SPRITES];
        public int[] sprnch = new int[SPRITES];
        public int[] sprnwid = new int[SPRITES];
        public int[] sprnhei = new int[SPRITES];
        public int[] sprnbwid = new int[SPRITES];
        public int[] sprnbhei = new int[SPRITES];

        private Game game;

        public Sprites(Game game)
        {
            this.game = game;
        }

        public void CreateSprite(int n, int ch, Surface mov, int wid, int hei, int bwid, int bhei)
        {
            sprnch[n] = sprch[n] = ch;
            sprmov[n] = mov;
            sprnwid[n] = sprwid[n] = wid;
            sprnhei[n] = sprhei[n] = hei;
            sprnbwid[n] = sprbwid[n] = bwid;
            sprnbhei[n] = sprbhei[n] = bhei;
            sprenf[n] = false;
        }

        public void MoveDrawSprite(int n, int x, int y)
        {
            sprx[n] = (short)(x & -4);
            spry[n] = y;
            sprch[n] = sprnch[n];
            sprwid[n] = sprnwid[n];
            sprhei[n] = sprnhei[n];
            sprbwid[n] = sprnbwid[n];
            sprbhei[n] = sprnbhei[n];
            clearrdrwf();
            setrdrwflgs(n);
            putis();
            game.video.GetImage(sprx[n], spry[n], ref sprmov[n], sprwid[n], sprhei[n]);
            sprenf[n] = true;
            sprrdrwf[n] = true;
            putims();
        }

        public void EraseSprite(int n)
        {
            if (!sprenf[n])
                return;
            game.video.PutImage(sprx[n], spry[n], sprmov[n], sprwid[n], sprhei[n]);
            sprenf[n] = false;
            clearrdrwf();
            setrdrwflgs(n);
            putims();
        }

        public void DrawSprite(int n, int x, int y)
        {
            x &= -4;
            clearrdrwf();
            setrdrwflgs(n);
            int t1 = sprx[n];
            int t2 = spry[n];
            int t3 = sprwid[n];
            int t4 = sprhei[n];
            sprx[n] = x;
            spry[n] = y;
            sprwid[n] = sprnwid[n];
            sprhei[n] = sprnhei[n];
            clearrecf();
            setrdrwflgs(n);
            sprhei[n] = t4;
            sprwid[n] = t3;
            spry[n] = t2;
            sprx[n] = t1;
            sprrdrwf[n] = true;
            putis();
            sprenf[n] = true;
            sprx[n] = x;
            spry[n] = y;
            sprch[n] = sprnch[n];
            sprwid[n] = sprnwid[n];
            sprhei[n] = sprnhei[n];
            sprbwid[n] = sprnbwid[n];
            sprbhei[n] = sprnbhei[n];
            game.video.GetImage(sprx[n], spry[n], ref sprmov[n], sprwid[n], sprhei[n]);

            putims();
            bcollides(n);
        }

        public void InitializeSprite(int n, int ch, int wid, int hei, short bwid, short bhei)
        {
            sprnch[n] = ch;
            sprnwid[n] = wid;
            sprnhei[n] = hei;
            sprnbwid[n] = bwid;
            sprnbhei[n] = bhei;
        }

        public void initmiscspr(int x, int y, int wid, int hei)
        {
            sprx[SPRITES] = x;
            spry[SPRITES] = y;
            sprwid[SPRITES] = wid;
            sprhei[SPRITES] = hei;
            clearrdrwf();
            setrdrwflgs(SPRITES);
            putis();
        }

        public void getis()
        {
            for (int i = 0; i < SPRITES; i++)
                if (sprrdrwf[i])
                    game.video.GetImage(sprx[i], spry[i], ref sprmov[i], sprwid[i], sprhei[i]);
            putims();
        }

        public void drawmiscspr(int x, int y, int ch, short wid, short hei)
        {
            sprx[SPRITES] = x & -4;
            spry[SPRITES] = y;
            sprch[SPRITES] = ch;
            sprwid[SPRITES] = wid;
            sprhei[SPRITES] = hei;
            game.video.PutImage(sprx[SPRITES], spry[SPRITES], sprch[SPRITES], sprwid[SPRITES], sprhei[SPRITES]);
        }

        public void clearrdrwf()
        {
            short i;
            clearrecf();
            for (i = 0; i < SPRITES + 1; i++)
                sprrdrwf[i] = false;
        }

        public void clearrecf()
        {
            short i;
            for (i = 0; i < SPRITES + 1; i++)
                sprrecf[i] = false;
        }

        public void setrdrwflgs(int n)
        {
            if (!sprrecf[n])
            {
                sprrecf[n] = true;
                for (int i = 0; i < SPRITES; i++)
                    if (sprenf[i] && i != n)
                    {
                        if (collide(i, n))
                        {
                            sprrdrwf[i] = true;
                            setrdrwflgs(i);
                        }
                    }
            }
        }

        public bool collide(int bx, int si)
        {
            if (sprx[bx] >= sprx[si])
            {
                if (sprx[bx] > (sprwid[si] << 2) + sprx[si] - 1)
                    return false;
            }
            else
              if (sprx[si] > (sprwid[bx] << 2) + sprx[bx] - 1)
                return false;
            if (spry[bx] >= spry[si])
            {
                if (spry[bx] <= sprhei[si] + spry[si] - 1)
                    return true;
                return false;
            }
            if (spry[si] <= sprhei[bx] + spry[bx] - 1)
                return true;
            return false;
        }

        public bool bcollide(int bx, int si)
        {
            if (sprx[bx] >= sprx[si])
            {
                if (sprx[bx] + sprbwid[bx] > (sprwid[si] << 2) + sprx[si] - sprbwid[si] - 1)
                    return false;
            }
            else
              if (sprx[si] + sprbwid[si] > (sprwid[bx] << 2) + sprx[bx] - sprbwid[bx] - 1)
                return false;
            if (spry[bx] >= spry[si])
            {
                if (spry[bx] + sprbhei[bx] <= sprhei[si] + spry[si] - sprbhei[si] - 1)
                    return true;
                return false;
            }
            if (spry[si] + sprbhei[si] <= sprhei[bx] + spry[bx] - sprbhei[bx] - 1)
                return true;
            return false;
        }

        public void putims()
        {
            for (int i = 0; i < SPRITES; i++)
                if (sprrdrwf[i])
                    game.video.PutImage(sprx[i], spry[i], sprch[i], sprwid[i], sprhei[i]);
        }

        public void putis()
        {
            for (int i = 0; i < SPRITES; i++)
                if (sprrdrwf[i])
                    game.video.PutImage(sprx[i], spry[i], sprmov[i], sprwid[i], sprhei[i]);
        }

        public int[] first = new int[Const.TYPES];
        public int[] coll = new int[SPRITES];
        public int[] firstt = { Const.FIRSTBONUS, Const.FIRSTBAG, Const.FIRSTMONSTER, Const.FIRSTFIREBALL, Const.FIRSTDIGGER };
        public int[] lastt = { Const.LASTBONUS, Const.LASTBAG, Const.LASTMONSTER, Const.LASTFIREBALL, Const.LASTDIGGER };

        public void bcollides(int spr)
        {
            int spc, next, i;
            for (next = 0; next < Const.TYPES; next++)
                first[next] = -1;
            for (next = 0; next < SPRITES; next++)
                coll[next] = -1;
            for (i = 0; i < Const.TYPES; i++)
            {
                next = -1;
                for (spc = firstt[i]; spc < lastt[i]; spc++)
                    if (sprenf[spc] && spc != spr)
                        if (bcollide(spr, spc))
                        {
                            if (next == -1)
                                first[i] = next = spc;
                            else
                                coll[next = (coll[next] = spc)] = -1;
                        }
            }
        }

        private void WriteDebug(int x, int y, char ch, short c)
        {
            System.Diagnostics.Debug.Assert(x + Const.CHR_W <= Const.MAX_W);
            System.Diagnostics.Debug.Assert(y + Const.CHR_H <= Const.MAX_H);
            game.video.WriteChar(x, y, ch, c);
        }
    }
}