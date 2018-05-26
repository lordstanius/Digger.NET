/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public struct Bag
        {
            public int x, y, h, v, xr, yr, dir, wt, gt, fallh;
            public bool wobbling, unfallen, exist;
        }

        public static Bag[] bagdat;
        public static Bag[] bagdat1;
        public static Bag[] bagdat2;

        public static int pushcount;
        public static int goldtime;

        public static void initbags()
        {
            short bag, x, y;
            pushcount = 0;
            goldtime = 150 - level.levof10() * 10;
            bagdat = new Bag[BAGS];
            for (bag = 0; bag < BAGS; bag++)
                bagdat[bag].exist = false;
            bag = 0;
            for (x = 0; x < MWIDTH; x++)
                for (y = 0; y < MHEIGHT; y++)
                    if (level.getlevch(x, y, level.levplan()) == 'B')
                        if (bag < BAGS)
                        {
                            bagdat[bag].exist = true;
                            bagdat[bag].gt = 0;
                            bagdat[bag].fallh = 0;
                            bagdat[bag].dir = DIR_NONE;
                            bagdat[bag].wobbling = false;
                            bagdat[bag].wt = 15;
                            bagdat[bag].unfallen = true;
                            bagdat[bag].x = x * 20 + 12;
                            bagdat[bag].y = y * 18 + 18;
                            bagdat[bag].h = x;
                            bagdat[bag].v = y;
                            bagdat[bag].xr = 0;
                            bagdat[bag++].yr = 0;
                        }
            if (g_CurrentPlayer == 0)
            {
                bagdat1 = new Bag[BAGS];
                bagdat.CopyTo(bagdat1, 0);
            }
            else
            {
                bagdat2 = new Bag[BAGS];
                bagdat.CopyTo(bagdat2, 0);
            }
        }

        public static void updatebag(SdlGraphics ddap, short bag)
        {
            int x, h, xr, y, v, yr, wbl;
            x = bagdat[bag].x;
            h = bagdat[bag].h;
            xr = bagdat[bag].xr;
            y = bagdat[bag].y;
            v = bagdat[bag].v;
            yr = bagdat[bag].yr;
            switch (bagdat[bag].dir)
            {
                case DIR_NONE:
                    if (y < 180 && xr == 0)
                    {
                        if (bagdat[bag].wobbling)
                        {
                            if (bagdat[bag].wt == 0)
                            {
                                bagdat[bag].dir = DIR_DOWN;
                                sound.soundfall();
                                break;
                            }
                            bagdat[bag].wt--;
                            wbl = bagdat[bag].wt % 8;
                            if ((wbl & 1) == 0)
                            {
                                drawApi.DrawGold(bag, wblanim[wbl >> 1], x, y);
                                incpenalty();
                                sound.soundwobble();
                            }
                        }
                        else
                          if ((monsters.getfield(h, v + 1) & 0xfdf) != 0xfdf)
                            if (!checkdiggerunderbag(h, v + 1))
                                bagdat[bag].wobbling = true;
                    }
                    else
                    {
                        bagdat[bag].wt = 15;
                        bagdat[bag].wobbling = false;
                    }
                    break;
                case DIR_RIGHT:
                case DIR_LEFT:
                    if (xr == 0)
                    {
                        if (y < 180 && (monsters.getfield(h, v + 1) & 0xfdf) != 0xfdf)
                        {
                            bagdat[bag].dir = DIR_DOWN;
                            bagdat[bag].wt = 0;
                            sound.soundfall();
                        }
                        else
                            baghitground(bag);
                    }
                    break;
                case DIR_DOWN:
                    if (yr == 0)
                        bagdat[bag].fallh++;
                    if (y >= 180)
                        baghitground(bag);
                    else
                      if ((monsters.getfield(h, v + 1) & 0xfdf) == 0xfdf)
                        if (yr == 0)
                            baghitground(bag);
                    monsters.checkmonscared(bagdat[bag].h);
                    break;
            }
            if (bagdat[bag].dir != DIR_NONE)
            {
                if (bagdat[bag].dir != DIR_DOWN && pushcount != 0)
                    pushcount--;
                else
                    pushbag(ddap, bag, bagdat[bag].dir);
            }
        }

        public static void drawbags()
        {
            short bag;
            for (bag = 0; bag < BAGS; bag++)
            {
                if (g_CurrentPlayer == 0)
                    bagdat[bag] = bagdat1[bag];
                else
                    bagdat[bag] = bagdat2[bag];

                if (bagdat[bag].exist)
                    sprites.movedrawspr(bag + FIRSTBAG, bagdat[bag].x, bagdat[bag].y);
            }
        }

        public static void cleanupbags()
        {
            short bag;
            sound.soundfalloff();
            for (bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].exist && ((bagdat[bag].h == 7 && bagdat[bag].v == 9) ||
                    bagdat[bag].xr != 0 || bagdat[bag].yr != 0 || bagdat[bag].gt != 0 ||
                    bagdat[bag].fallh != 0 || bagdat[bag].wobbling))
                {
                    bagdat[bag].exist = false;
                    sprites.erasespr(bag + FIRSTBAG);
                }
                if (g_CurrentPlayer == 0)
                    bagdat1[bag] = bagdat[bag];
                else
                    bagdat2[bag] = bagdat[bag];
            }
        }

        public static void dobags(SdlGraphics ddap)
        {
            bool soundfalloffflag = true, soundwobbleoffflag = true;
            for (int bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].exist)
                {
                    if (bagdat[bag].gt != 0)
                    {
                        if (bagdat[bag].gt == 1)
                        {
                            sound.soundbreak();
                            drawApi.DrawGold(bag, 4, bagdat[bag].x, bagdat[bag].y);
                            incpenalty();
                        }
                        if (bagdat[bag].gt == 3)
                        {
                            drawApi.DrawGold(bag, 5, bagdat[bag].x, bagdat[bag].y);
                            incpenalty();
                        }
                        if (bagdat[bag].gt == 5)
                        {
                            drawApi.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
                            incpenalty();
                        }
                        bagdat[bag].gt++;
                        if (bagdat[bag].gt == goldtime)
                            removebag(bag);
                        else
                          if (bagdat[bag].v < MHEIGHT - 1 && bagdat[bag].gt < goldtime - 10)
                            if ((monsters.getfield(bagdat[bag].h, bagdat[bag].v + 1) & 0x2000) == 0)
                                bagdat[bag].gt = goldtime - 10;
                    }
                    else
                        updatebag(ddap, bag);
                }
            }

            for (int bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].dir == DIR_DOWN && bagdat[bag].exist)
                    soundfalloffflag = false;
                if (bagdat[bag].dir != DIR_DOWN && bagdat[bag].wobbling && bagdat[bag].exist)
                    soundwobbleoffflag = false;
            }

            if (soundfalloffflag)
                sound.soundfalloff();
            if (soundwobbleoffflag)
                sound.soundwobbleoff();
        }

        private static int[] wblanim = { 2, 0, 1, 0 };

        public static void updatebag(SdlGraphics ddap, int bag)
        {
            int x, h, xr, y, v, yr, wbl;
            x = bagdat[bag].x;
            h = bagdat[bag].h;
            xr = bagdat[bag].xr;
            y = bagdat[bag].y;
            v = bagdat[bag].v;
            yr = bagdat[bag].yr;
            switch (bagdat[bag].dir)
            {
                case DIR_NONE:
                    if (y < 180 && xr == 0)
                    {
                        if (bagdat[bag].wobbling)
                        {
                            if (bagdat[bag].wt == 0)
                            {
                                bagdat[bag].dir = DIR_DOWN;
                                sound.soundfall();
                                break;
                            }
                            bagdat[bag].wt--;
                            wbl = bagdat[bag].wt % 8;
                            if ((wbl & 1) == 0)
                            {
                                drawApi.DrawGold(bag, wblanim[wbl >> 1], x, y);
                                incpenalty();
                                sound.soundwobble();
                            }
                        }
                        else
                          if ((monsters.getfield(h, v + 1) & 0xfdf) != 0xfdf)
                            if (!checkdiggerunderbag(h, v + 1))
                                bagdat[bag].wobbling = true;
                    }
                    else
                    {
                        bagdat[bag].wt = 15;
                        bagdat[bag].wobbling = false;
                    }
                    break;
                case DIR_RIGHT:
                case DIR_LEFT:
                    if (xr == 0)
                    {
                        if (y < 180 && (monsters.getfield(h, v + 1) & 0xfdf) != 0xfdf)
                        {
                            bagdat[bag].dir = DIR_DOWN;
                            bagdat[bag].wt = 0;
                            sound.soundfall();
                        }
                        else
                            baghitground(bag);
                    }
                    break;
                case DIR_DOWN:
                    if (yr == 0)
                        bagdat[bag].fallh++;
                    if (y >= 180)
                        baghitground(bag);
                    else
                      if ((monsters.getfield(h, v + 1) & 0xfdf) == 0xfdf)
                        if (yr == 0)
                            baghitground(bag);
                    monsters.checkmonscared(bagdat[bag].h);
                    break;
            }
            if (bagdat[bag].dir != DIR_NONE)
            {
                if (bagdat[bag].dir != DIR_DOWN && pushcount != 0)
                    pushcount--;
                else
                    pushbag(ddap, bag, bagdat[bag].dir);
            }
        }

        private static void baghitground(int bag)
        {
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            if (bagdat[bag].dir == DIR_DOWN && bagdat[bag].fallh > 1)
                bagdat[bag].gt = 1;
            else
                bagdat[bag].fallh = 0;
            bagdat[bag].dir = DIR_NONE;
            bagdat[bag].wt = 15;
            bagdat[bag].wobbling = false;
            drawApi.DrawGold(bag, 0, bagdat[bag].x, bagdat[bag].y);
            for (int i = 0; i < TYPES; i++)
                clfirst[i] = sprites.first[i];
            for (int i = 0; i < SPRITES; i++)
                clcoll[i] = sprites.coll[i];
            incpenalty();
            int j = clfirst[1];
            while (j != -1)
            {
                removebag(j - FIRSTBAG);
                j = clcoll[j];
            }
        }

        private static bool pushbag(SdlGraphics ddap, int bag, int dir)
        {
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            bool push = true, digf;
            int ox = bagdat[bag].x;
            int x = bagdat[bag].x;
            int oy = bagdat[bag].y;
            int y = bagdat[bag].y;
            int h = bagdat[bag].h;
            int v = bagdat[bag].v;
            int i = 0;
            if (bagdat[bag].gt != 0)
            {
                getgold(ddap, bag);
                return true;
            }
            if (bagdat[bag].dir == DIR_DOWN && (dir == DIR_RIGHT || dir == DIR_LEFT))
            {
                drawApi.DrawGold(bag, 3, x, y);
                for (i = 0; i < TYPES; i++)
                    clfirst[i] = sprites.first[i];
                for (i = 0; i < SPRITES; i++)
                    clcoll[i] = sprites.coll[i];
                incpenalty();
                i = clfirst[4];
                while (i != -1)
                {
                    if (diggery(i - FIRSTDIGGER + g_CurrentPlayer) >= y)
                        killdigger(i - FIRSTDIGGER + g_CurrentPlayer, 1, bag);
                    i = clcoll[i];
                }
                if (clfirst[2] != -1)
                    monsters.squashmonsters(bag, clfirst, clcoll);
                return true;
            }
            if ((x == 292 && dir == DIR_RIGHT) || (x == 12 && dir == DIR_LEFT) ||
                (y == 180 && dir == DIR_DOWN) || (y == 18 && dir == DIR_UP))
                push = false;
            if (push)
            {
                switch (dir)
                {
                    case DIR_RIGHT:
                        x += 4;
                        break;
                    case DIR_LEFT:
                        x -= 4;
                        break;
                    case DIR_DOWN:
                        if (bagdat[bag].unfallen)
                        {
                            bagdat[bag].unfallen = false;
                            drawApi.drawsquareblob(x, y);
                            drawApi.drawtopblob(x, y + 21);
                        }
                        else
                            drawApi.drawfurryblob(x, y);
                        drawApi.EatField(x, y, dir);
                        killemerald(h, v);
                        y += 6;
                        break;
                }
                switch (dir)
                {
                    case DIR_DOWN:
                        drawApi.DrawGold(bag, 3, x, y);
                        for (i = 0; i < TYPES; i++)
                            clfirst[i] = sprites.first[i];
                        for (i = 0; i < SPRITES; i++)
                            clcoll[i] = sprites.coll[i];
                        incpenalty();
                        i = clfirst[4];
                        while (i != -1)
                        {
                            if (diggery(i - FIRSTDIGGER + g_CurrentPlayer) >= y)
                                killdigger(i - FIRSTDIGGER + g_CurrentPlayer, 1, bag);
                            i = clcoll[i];
                        }
                        if (clfirst[2] != -1)
                            monsters.squashmonsters(bag, clfirst, clcoll);
                        break;
                    case DIR_RIGHT:
                    case DIR_LEFT:
                        bagdat[bag].wt = 15;
                        bagdat[bag].wobbling = false;
                        drawApi.DrawGold(bag, 0, x, y);
                        for (i = 0; i < TYPES; i++)
                            clfirst[i] = sprites.first[i];
                        for (i = 0; i < SPRITES; i++)
                            clcoll[i] = sprites.coll[i];
                        incpenalty();
                        pushcount = 1;
                        if (clfirst[1] != -1)
                            if (!pushbags(ddap, dir, clfirst, clcoll))
                            {
                                x = ox;
                                y = oy;
                                drawApi.DrawGold(bag, 0, ox, oy);
                                incpenalty();
                                push = false;
                            }
                        i = clfirst[4];
                        digf = false;
                        while (i != -1)
                        {
                            if (digalive(i - FIRSTDIGGER + g_CurrentPlayer))
                                digf = true;
                            i = clcoll[i];
                        }
                        if (digf || clfirst[2] != -1)
                        {
                            x = ox;
                            y = oy;
                            drawApi.DrawGold(bag, 0, ox, oy);
                            incpenalty();
                            push = false;
                        }
                        break;
                }
                if (push)
                    bagdat[bag].dir = dir;
                else
                    bagdat[bag].dir = reversedir(dir);
                bagdat[bag].x = x;
                bagdat[bag].y = y;
                bagdat[bag].h = (x - 12) / 20;
                bagdat[bag].v = (y - 18) / 18;
                bagdat[bag].xr = (x - 12) % 20;
                bagdat[bag].yr = (y - 18) % 18;
            }
            return push;
        }

        public static bool pushbags(SdlGraphics ddap, int dir, int[] clfirst, int[] clcoll)
        {
            bool push = true;
            int next = clfirst[1];
            while (next != -1)
            {
                if (!pushbag(ddap, next - FIRSTBAG, dir))
                    push = false;
                next = clcoll[next];
            }
            return push;
        }

        public static bool pushudbags(SdlGraphics ddap, int[] clfirst, int[] clcoll)
        {
            bool push = true;
            int next = clfirst[1];
            while (next != -1)
            {
                if (bagdat[next - FIRSTBAG].gt != 0)
                    getgold(ddap, next - FIRSTBAG);
                else
                    push = false;
                next = clcoll[next];
            }
            return push;
        }

        private static void removebag(int bag)
        {
            if (bagdat[bag].exist)
            {
                bagdat[bag].exist = false;
                sprites.erasespr(bag + FIRSTBAG);
            }
        }

        public static bool bagexist(int bag)
        {
            return bagdat[bag].exist;
        }

        public static int bagy(int bag)
        {
            return bagdat[bag].y;
        }

        public static int getbagdir(int bag)
        {
            if (bagdat[bag].exist)
                return bagdat[bag].dir;
            return -1;
        }

        public static void removebags(int[] clfirst, int[] clcoll)
        {
            int next = clfirst[1];
            while (next != -1)
            {
                removebag(next - FIRSTBAG);
                next = clcoll[next];
            }
        }

        public static short getnmovingbags()
        {
            short bag, n = 0;
            for (bag = 0; bag < BAGS; bag++)
                if (bagdat[bag].exist && bagdat[bag].gt < 10 &&
                    (bagdat[bag].gt != 0 || bagdat[bag].wobbling))
                    n++;
            return n;
        }

        private static void getgold(SdlGraphics ddap, int bag)
        {
            bool f = true;
            int i;
            drawApi.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
            incpenalty();
            i = sprites.first[4];
            while (i != -1)
            {
                if (digalive(i - FIRSTDIGGER + g_CurrentPlayer))
                {
                    scores.scoregold(ddap, i - FIRSTDIGGER + g_CurrentPlayer);
                    sound.soundgold();
                    digresettime(i - FIRSTDIGGER + g_CurrentPlayer);
                    f = false;
                }
                i = sprites.coll[i];
            }
            if (f)
                monsters.mongold();
            removebag(bag);
        }

    }
}