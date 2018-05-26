/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public class Bags
    {
        public struct Bag
        {
            public int x, y, h, v, xr, yr, dir, wt, gt, fallh, id;
            public bool wobbling, unfallen, exist;
        }

        public Bag[] bagdat = new Bag[BAGS];
        public Bag[] bagdat1 = new Bag[BAGS];
        public Bag[] bagdat2 = new Bag[BAGS];

        public int pushcount;
        public int goldtime;

        public Bag Current;

        private const int BAGS = DiggerC.BAGS;
        private const int MWIDTH = DiggerC.MWIDTH;
        private const int MHEIGHT = DiggerC.MHEIGHT;
        private const int DIR_NONE = DiggerC.DIR_NONE;
        private const int DIR_RIGHT = DiggerC.DIR_RIGHT;
        private const int DIR_UP = DiggerC.DIR_UP;
        private const int DIR_LEFT = DiggerC.DIR_LEFT;
        private const int DIR_DOWN = DiggerC.DIR_DOWN;
        private const int FIRSTBAG = DiggerC.FIRSTBAG;
        private const int MAX_W = DiggerC.MAX_W;
        private const int MAX_H = DiggerC.MAX_H;
        private const int CHR_W = DiggerC.CHR_W;
        private const int CHR_H = DiggerC.CHR_H;
        private const int TYPES = DiggerC.TYPES;
        private const int SPRITES = DiggerC.SPRITES;
        private const int FIRSTDIGGER = DiggerC.FIRSTDIGGER;

        private Level level;
        private Sound sound;
        private DrawApi drawApi;
        private Monsters monsters;
        private Sprites sprites;
        private Scores scores;

        public Bags(Level level, Sound sound, DrawApi drawApi, Monsters monsters, Sprites sprites, Scores scores)
        {
            this.level = level;
            this.sound = sound;
            this.drawApi = drawApi;
            this.monsters = monsters;
            this.sprites = sprites;
            this.scores = scores;
        }

        public void Initialize()
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
                            bagdat[bag].id = bag;
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
            if (DiggerC.g_CurrentPlayer == 0)
                bagdat.CopyTo(bagdat1, 0);
            else
                bagdat.CopyTo(bagdat2, 0);
        }

        public void DrawBags()
        {
            short bag;
            for (bag = 0; bag < BAGS; bag++)
            {
                if (DiggerC.g_CurrentPlayer == 0)
                    bagdat[bag] = bagdat1[bag];
                else
                    bagdat[bag] = bagdat2[bag];

                if (bagdat[bag].exist)
                    sprites.movedrawspr(bag + FIRSTBAG, bagdat[bag].x, bagdat[bag].y);
            }
        }

        public void Cleanup()
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
                if (DiggerC.g_CurrentPlayer == 0)
                    bagdat1[bag] = bagdat[bag];
                else
                    bagdat2[bag] = bagdat[bag];
            }
        }

        public void DoBags()
        {
            bool soundfalloffflag = true, soundwobbleoffflag = true;
            for (int bag = 0; bag < BAGS; ++bag)
            {
                Current = bagdat[bag];
                if (bagdat[bag].exist)
                {
                    if (bagdat[bag].gt != 0)
                    {
                        if (bagdat[bag].gt == 1)
                        {
                            sound.soundbreak();
                            drawApi.DrawGold(bag, 4, bagdat[bag].x, bagdat[bag].y);
                            DiggerC.incpenalty();
                        }
                        if (bagdat[bag].gt == 3)
                        {
                            drawApi.DrawGold(bag, 5, bagdat[bag].x, bagdat[bag].y);
                            DiggerC.incpenalty();
                        }
                        if (bagdat[bag].gt == 5)
                        {
                            drawApi.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
                            DiggerC.incpenalty();
                        }
                        bagdat[bag].gt++;
                        if (bagdat[bag].gt == goldtime)
                            RemoveBag(bag);
                        else
                          if (bagdat[bag].v < MHEIGHT - 1 && bagdat[bag].gt < goldtime - 10)
                            if ((monsters.getfield(bagdat[bag].h, bagdat[bag].v + 1) & 0x2000) == 0)
                                bagdat[bag].gt = goldtime - 10;
                    }
                    else
                        UpdateBag(bag);
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

        private int[] wblanim = { 2, 0, 1, 0 };

        public void UpdateBag(int bag)
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
                                DiggerC.incpenalty();
                                sound.soundwobble();
                            }
                        }
                        else
                          if ((monsters.getfield(h, v + 1) & 0xfdf) != 0xfdf)
                            if (!DiggerC.checkdiggerunderbag(h, v + 1))
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
                            OnBagHitsTheGround(bag);
                    }
                    break;
                case DIR_DOWN:
                    if (yr == 0)
                        bagdat[bag].fallh++;
                    if (y >= 180)
                        OnBagHitsTheGround(bag);
                    else
                      if ((monsters.getfield(h, v + 1) & 0xfdf) == 0xfdf)
                        if (yr == 0)
                            OnBagHitsTheGround(bag);
                    monsters.checkmonscared(bagdat[bag].h);
                    break;
            }
            if (bagdat[bag].dir != DIR_NONE)
            {
                if (bagdat[bag].dir != DIR_DOWN && pushcount != 0)
                    pushcount--;
                else
                    PushBag(bag, bagdat[bag].dir);
            }
        }

        private void OnBagHitsTheGround(int bag)
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
            DiggerC.incpenalty();
            int j = clfirst[1];
            while (j != -1)
            {
                RemoveBag(j - FIRSTBAG);
                j = clcoll[j];
            }
        }

        private bool PushBag(int bag, int dir)
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
                GetGold(bag);
                return true;
            }
            if (bagdat[bag].dir == DIR_DOWN && (dir == DIR_RIGHT || dir == DIR_LEFT))
            {
                drawApi.DrawGold(bag, 3, x, y);
                for (i = 0; i < TYPES; i++)
                    clfirst[i] = sprites.first[i];
                for (i = 0; i < SPRITES; i++)
                    clcoll[i] = sprites.coll[i];
                DiggerC.incpenalty();
                i = clfirst[4];
                while (i != -1)
                {
                    if (DiggerC.diggery(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer) >= y)
                        DiggerC.killdigger(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer, 1, bag);
                    i = clcoll[i];
                }
                if (clfirst[2] != -1)
                    monsters.SquashMonsters(this, clfirst, clcoll);
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
                        DiggerC.killemerald(h, v);
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
                        DiggerC.incpenalty();
                        i = clfirst[4];
                        while (i != -1)
                        {
                            if (DiggerC.diggery(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer) >= y)
                                DiggerC.killdigger(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer, 1, bag);
                            i = clcoll[i];
                        }
                        if (clfirst[2] != -1)
                            monsters.SquashMonsters(this, clfirst, clcoll);
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
                        DiggerC.incpenalty();
                        pushcount = 1;
                        if (clfirst[1] != -1)
                            if (!PushBags(dir, clfirst, clcoll))
                            {
                                x = ox;
                                y = oy;
                                drawApi.DrawGold(bag, 0, ox, oy);
                                DiggerC.incpenalty();
                                push = false;
                            }
                        i = clfirst[4];
                        digf = false;
                        while (i != -1)
                        {
                            if (DiggerC.digalive(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer))
                                digf = true;
                            i = clcoll[i];
                        }
                        if (digf || clfirst[2] != -1)
                        {
                            x = ox;
                            y = oy;
                            drawApi.DrawGold(bag, 0, ox, oy);
                            DiggerC.incpenalty();
                            push = false;
                        }
                        break;
                }
                if (push)
                    bagdat[bag].dir = dir;
                else
                    bagdat[bag].dir = DiggerC.reversedir(dir);
                bagdat[bag].x = x;
                bagdat[bag].y = y;
                bagdat[bag].h = (x - 12) / 20;
                bagdat[bag].v = (y - 18) / 18;
                bagdat[bag].xr = (x - 12) % 20;
                bagdat[bag].yr = (y - 18) % 18;
            }
            return push;
        }

        public bool PushBags(int dir, int[] clfirst, int[] clcoll)
        {
            bool push = true;
            int next = clfirst[1];
            while (next != -1)
            {
                if (!PushBag(next - FIRSTBAG, dir))
                    push = false;
                next = clcoll[next];
            }
            return push;
        }

        public bool PushBagsUp(int[] clfirst, int[] clcoll)
        {
            bool push = true;
            int next = clfirst[1];
            while (next != -1)
            {
                if (bagdat[next - FIRSTBAG].gt != 0)
                    GetGold(next - FIRSTBAG);
                else
                    push = false;
                next = clcoll[next];
            }
            return push;
        }

        private void RemoveBag(int bag)
        {
            if (bagdat[bag].exist)
            {
                bagdat[bag].exist = false;
                sprites.erasespr(bag + FIRSTBAG);
            }
        }

        public bool BagExists(int bag)
        {
            return bagdat[bag].exist;
        }

        public int BagY(int bag)
        {
            return bagdat[bag].y;
        }

        public int GetBagDir(int bag)
        {
            if (bagdat[bag].exist)
                return bagdat[bag].dir;
            return -1;
        }

        public void RemoveBags(int[] clfirst, int[] clcoll)
        {
            int next = clfirst[1];
            while (next != -1)
            {
                RemoveBag(next - FIRSTBAG);
                next = clcoll[next];
            }
        }

        public short GetNotMovingBags()
        {
            short bag, n = 0;
            for (bag = 0; bag < BAGS; bag++)
                if (bagdat[bag].exist && bagdat[bag].gt < 10 &&
                    (bagdat[bag].gt != 0 || bagdat[bag].wobbling))
                    n++;
            return n;
        }

        private void GetGold(int bag)
        {
            bool f = true;
            int i;
            drawApi.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
            DiggerC.incpenalty();
            i = sprites.first[4];
            while (i != -1)
            {
                if (DiggerC.digalive(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer))
                {
                    scores.scoregold(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer);
                    sound.soundgold();
                    DiggerC.digresettime(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer);
                    f = false;
                }
                i = sprites.coll[i];
            }
            if (f)
                monsters.mongold();

            RemoveBag(bag);
        }
    }
}