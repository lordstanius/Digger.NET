/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using SDL2;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public static bool retrflag = true;

        public static bool[] sprrdrwf = new bool[SPRITES + 1];
        public static bool[] sprrecf = new bool[SPRITES + 1];
        public static bool[] sprenf = new bool[SPRITES];
        public static Surface[] sprmov = new Surface[SPRITES];
        public static int[] sprch = new int[SPRITES + 1];
        public static int[] sprx = new int[SPRITES + 1];
        public static int[] spry = new int[SPRITES + 1];
        public static int[] sprwid = new int[SPRITES + 1];
        public static int[] sprhei = new int[SPRITES + 1];
        public static int[] sprbwid = new int[SPRITES];
        public static int[] sprbhei = new int[SPRITES];
        public static int[] sprnch = new int[SPRITES];
        public static int[] sprnwid = new int[SPRITES];
        public static int[] sprnhei = new int[SPRITES];
        public static int[] sprnbwid = new int[SPRITES];
        public static int[] sprnbhei = new int[SPRITES];

        private static digger_draw_api dda_static = new digger_draw_api
        {
            init = vgainit,
            clear = vgaclear,
            pal = vgapal,
            inten = vgainten,
            puti = vgaputi,
            geti = vgageti,
            putim = vgaputim,
            getpix = vgagetpix,
            title = vgatitle,
#if !DIGGER_DEBUG
            write = vgawrite,
#else
            write = gwrite_debug,
#endif
            flush = doscreenupdate
        };

        public static digger_draw_api ddap = dda_static;

        public static void setretr(bool f)
        {
            retrflag = f;
        }

        public static void createspr(int n, int ch, Surface mov, int wid, int hei, int bwid, int bhei)
        {
            sprnch[n] = sprch[n] = ch;
            sprmov[n] = mov;
            sprnwid[n] = sprwid[n] = wid;
            sprnhei[n] = sprhei[n] = hei;
            sprnbwid[n] = sprbwid[n] = bwid;
            sprnbhei[n] = sprbhei[n] = bhei;
            sprenf[n] = false;
        }

        public static void movedrawspr(int n, int x, int y)
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
            ddap.geti(sprx[n], spry[n], ref sprmov[n], sprwid[n], sprhei[n]);
            sprenf[n] = true;
            sprrdrwf[n] = true;
            putims();
        }

        public static void erasespr(int n)
        {
            if (!sprenf[n])
                return;
            ddap.puti(sprx[n], spry[n], sprmov[n], sprwid[n], sprhei[n]);
            sprenf[n] = false;
            clearrdrwf();
            setrdrwflgs(n);
            putims();
        }

        public static void drawspr(int n, int x, int y)
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
            ddap.geti(sprx[n], spry[n], ref sprmov[n], sprwid[n], sprhei[n]);

            putims();
            bcollides(n);
        }

        public static void initspr(int n, int ch, int wid, int hei, short bwid, short bhei)
        {
            sprnch[n] = ch;
            sprnwid[n] = wid;
            sprnhei[n] = hei;
            sprnbwid[n] = bwid;
            sprnbhei[n] = bhei;
        }

        public static void initmiscspr(int x, int y, int wid, int hei)
        {
            sprx[SPRITES] = x;
            spry[SPRITES] = y;
            sprwid[SPRITES] = wid;
            sprhei[SPRITES] = hei;
            clearrdrwf();
            setrdrwflgs(SPRITES);
            putis();
        }

        public static void getis()
        {
            for (int i = 0; i < SPRITES; i++)
                if (sprrdrwf[i])
                    ddap.geti(sprx[i], spry[i], ref sprmov[i], sprwid[i], sprhei[i]);
            putims();
        }

        public static void drawmiscspr(int x, int y, int ch, short wid, short hei)
        {
            sprx[SPRITES] = x & -4;
            spry[SPRITES] = y;
            sprch[SPRITES] = ch;
            sprwid[SPRITES] = wid;
            sprhei[SPRITES] = hei;
            ddap.putim(sprx[SPRITES], spry[SPRITES], sprch[SPRITES], sprwid[SPRITES], sprhei[SPRITES]);
        }

        public static void clearrdrwf()
        {
            short i;
            clearrecf();
            for (i = 0; i < SPRITES + 1; i++)
                sprrdrwf[i] = false;
        }

        public static void clearrecf()
        {
            short i;
            for (i = 0; i < SPRITES + 1; i++)
                sprrecf[i] = false;
        }

        public static void setrdrwflgs(int n)
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

        public static bool collide(int bx, int si)
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

        public static bool bcollide(int bx, int si)
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

        public static void putims()
        {
            for (int i = 0; i < SPRITES; i++)
                if (sprrdrwf[i])
                    ddap.putim(sprx[i], spry[i], sprch[i], sprwid[i], sprhei[i]);
        }

        public static void putis()
        {
            for (int i = 0; i < SPRITES; i++)
                if (sprrdrwf[i])
                    ddap.puti(sprx[i], spry[i], sprmov[i], sprwid[i], sprhei[i]);
        }

        public static int[] first = new int[TYPES];
        public static int[] coll = new int[SPRITES];
        public static int[] firstt = { FIRSTBONUS, FIRSTBAG, FIRSTMONSTER, FIRSTFIREBALL, FIRSTDIGGER };
        public static int[] lastt = { LASTBONUS, LASTBAG, LASTMONSTER, LASTFIREBALL, LASTDIGGER };

        public static void bcollides(int spr)
        {
            int spc, next, i;
            for (next = 0; next < TYPES; next++)
                first[next] = -1;
            for (next = 0; next < SPRITES; next++)
                coll[next] = -1;
            for (i = 0; i < TYPES; i++)
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

        static void gwrite_debug(int x, int y, char ch, short c)
        {
            System.Diagnostics.Debug.Assert(x + CHR_W <= MAX_W);
            System.Diagnostics.Debug.Assert(y + CHR_H <= MAX_H);
            vgawrite(x, y, ch, c);
        }
    }
}