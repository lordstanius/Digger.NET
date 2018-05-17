/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using SDL2;

namespace Digger.Net
{
    public class Sprites
    {
        public bool[] sprrdrwf = new bool[DiggerC.SPRITES + 1];
        public bool[] sprrecf = new bool[DiggerC.SPRITES + 1];
        public bool[] sprenf = new bool[DiggerC.SPRITES];
        public Surface[] sprmov = new Surface[DiggerC.SPRITES];
        public int[] sprch = new int[DiggerC.SPRITES + 1];
        public int[] sprx = new int[DiggerC.SPRITES + 1];
        public int[] spry = new int[DiggerC.SPRITES + 1];
        public int[] sprwid = new int[DiggerC.SPRITES + 1];
        public int[] sprhei = new int[DiggerC.SPRITES + 1];
        public int[] sprbwid = new int[DiggerC.SPRITES];
        public int[] sprbhei = new int[DiggerC.SPRITES];
        public int[] sprnch = new int[DiggerC.SPRITES];
        public int[] sprnwid = new int[DiggerC.SPRITES];
        public int[] sprnhei = new int[DiggerC.SPRITES];
        public int[] sprnbwid = new int[DiggerC.SPRITES];
        public int[] sprnbhei = new int[DiggerC.SPRITES];

        private SdlGraphics ddap;

        public Sprites(SdlGraphics ddap)
        {
            this.ddap = ddap;
        }

        public void createspr(int n, int ch, Surface mov, int wid, int hei, int bwid, int bhei)
        {
            sprnch[n] = sprch[n] = ch;
            sprmov[n] = mov;
            sprnwid[n] = sprwid[n] = wid;
            sprnhei[n] = sprhei[n] = hei;
            sprnbwid[n] = sprbwid[n] = bwid;
            sprnbhei[n] = sprbhei[n] = bhei;
            sprenf[n] = false;
        }

        public void movedrawspr(int n, int x, int y)
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
            ddap.GetImage(sprx[n], spry[n], ref sprmov[n], sprwid[n], sprhei[n]);
            sprenf[n] = true;
            sprrdrwf[n] = true;
            putims();
        }

        public void erasespr(int n)
        {
            if (!sprenf[n])
                return;
            ddap.PutImage(sprx[n], spry[n], sprmov[n], sprwid[n], sprhei[n]);
            sprenf[n] = false;
            clearrdrwf();
            setrdrwflgs(n);
            putims();
        }

        public void drawspr(int n, int x, int y)
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
            ddap.GetImage(sprx[n], spry[n], ref sprmov[n], sprwid[n], sprhei[n]);

            putims();
            bcollides(n);
        }

        public void initspr(int n, int ch, int wid, int hei, short bwid, short bhei)
        {
            sprnch[n] = ch;
            sprnwid[n] = wid;
            sprnhei[n] = hei;
            sprnbwid[n] = bwid;
            sprnbhei[n] = bhei;
        }

        public void initmiscspr(int x, int y, int wid, int hei)
        {
            sprx[DiggerC.SPRITES] = x;
            spry[DiggerC.SPRITES] = y;
            sprwid[DiggerC.SPRITES] = wid;
            sprhei[DiggerC.SPRITES] = hei;
            clearrdrwf();
            setrdrwflgs(DiggerC.SPRITES);
            putis();
        }

        public void getis()
        {
            for (int i = 0; i < DiggerC.SPRITES; i++)
                if (sprrdrwf[i])
                    ddap.GetImage(sprx[i], spry[i], ref sprmov[i], sprwid[i], sprhei[i]);
            putims();
        }

        public void drawmiscspr(int x, int y, int ch, short wid, short hei)
        {
            sprx[DiggerC.SPRITES] = x & -4;
            spry[DiggerC.SPRITES] = y;
            sprch[DiggerC.SPRITES] = ch;
            sprwid[DiggerC.SPRITES] = wid;
            sprhei[DiggerC.SPRITES] = hei;
            ddap.PutImage(sprx[DiggerC.SPRITES], spry[DiggerC.SPRITES], sprch[DiggerC.SPRITES], sprwid[DiggerC.SPRITES], sprhei[DiggerC.SPRITES]);
        }

        public void clearrdrwf()
        {
            short i;
            clearrecf();
            for (i = 0; i < DiggerC.SPRITES + 1; i++)
                sprrdrwf[i] = false;
        }

        public void clearrecf()
        {
            short i;
            for (i = 0; i < DiggerC.SPRITES + 1; i++)
                sprrecf[i] = false;
        }

        public void setrdrwflgs(int n)
        {
            if (!sprrecf[n])
            {
                sprrecf[n] = true;
                for (int i = 0; i < DiggerC.SPRITES; i++)
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
            for (int i = 0; i < DiggerC.SPRITES; i++)
                if (sprrdrwf[i])
                    ddap.PutImage(sprx[i], spry[i], sprch[i], sprwid[i], sprhei[i]);
        }

        public void putis()
        {
            for (int i = 0; i < DiggerC.SPRITES; i++)
                if (sprrdrwf[i])
                    ddap.PutImage(sprx[i], spry[i], sprmov[i], sprwid[i], sprhei[i]);
        }

        public int[] first = new int[DiggerC.TYPES];
        public int[] coll = new int[DiggerC.SPRITES];
        public int[] firstt = { DiggerC.FIRSTBONUS, DiggerC.FIRSTBAG, DiggerC.FIRSTMONSTER, DiggerC.FIRSTFIREBALL, DiggerC.FIRSTDIGGER };
        public int[] lastt = { DiggerC.LASTBONUS, DiggerC.LASTBAG, DiggerC.LASTMONSTER, DiggerC.LASTFIREBALL, DiggerC.LASTDIGGER };

        public void bcollides(int spr)
        {
            int spc, next, i;
            for (next = 0; next < DiggerC.TYPES; next++)
                first[next] = -1;
            for (next = 0; next < DiggerC.SPRITES; next++)
                coll[next] = -1;
            for (i = 0; i < DiggerC.TYPES; i++)
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

        void gwrite_debug(int x, int y, char ch, short c)
        {
            System.Diagnostics.Debug.Assert(x + DiggerC.CHR_W <= DiggerC.MAX_W);
            System.Diagnostics.Debug.Assert(y + DiggerC.CHR_H <= DiggerC.MAX_H);
            ddap.WriteChar(x, y, ch, c);
        }
    }
}