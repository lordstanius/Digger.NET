/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public struct Bag
    {
        public int x, y, h, v, xr, yr, dir, wt, gt, fallh;
        public bool wobbling, unfallen, Exists;
    }

    public class Bags
    {
        public Bag[] bagdat = new Bag[BAGS];
        public Bag[] bagdat1 = new Bag[BAGS];
        public Bag[] bagdat2 = new Bag[BAGS];

        public int pushcount;
        public int goldtime;

        private const int BAGS = Const.BAGS;
        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int DIR_NONE = Const.DIR_NONE;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;
        private const int FIRSTBAG = Const.FIRSTBAG;
        private const int MAX_W = Const.MAX_W;
        private const int MAX_H = Const.MAX_H;
        private const int CHR_W = Const.CHR_W;
        private const int CHR_H = Const.CHR_H;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;

        private readonly Game game;
        private readonly Level level;
        private readonly Sound sound;
        private readonly DrawApi drawApi;
        private readonly Monsters monsters;
        private readonly Sprites sprites;
        private readonly Scores scores;
        private readonly Diggers diggers;

        public Bags(Game game)
        {
            this.game = game;
            this.level = game.level;
            this.sound = game.sound;
            this.drawApi = game.drawApi;
            this.monsters = game.monsters;
            this.sprites = game.sprites;
            this.scores = game.scores;
            this.diggers = game.diggers;
        }

        public void Initialize()
        {
            short bag, x, y;
            pushcount = 0;
            goldtime = 150 - level.LevelOf10() * 10;
            bagdat = new Bag[BAGS];
            for (bag = 0; bag < BAGS; bag++)
                bagdat[bag].Exists = false;
            bag = 0;
            for (x = 0; x < MWIDTH; x++)
                for (y = 0; y < MHEIGHT; y++)
                    if (level.GetLevelChar(x, y, level.LevelPlan()) == 'B')
                        if (bag < BAGS)
                        {
                            bagdat[bag].Exists = true;
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
            if (game.CurrentPlayer == 0)
                bagdat.CopyTo(bagdat1, 0);
            else
                bagdat.CopyTo(bagdat2, 0);
        }

        public void DrawBags()
        {
            short bag;
            for (bag = 0; bag < BAGS; bag++)
            {
                if (game.CurrentPlayer == 0)
                    bagdat[bag] = bagdat1[bag];
                else
                    bagdat[bag] = bagdat2[bag];

                if (bagdat[bag].Exists)
                    sprites.MoveDrawSprite(bag + FIRSTBAG, bagdat[bag].x, bagdat[bag].y);
            }
        }

        public void Cleanup()
        {
            short bag;
            sound.soundfalloff();
            for (bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].Exists && ((bagdat[bag].h == 7 && bagdat[bag].v == 9) ||
                    bagdat[bag].xr != 0 || bagdat[bag].yr != 0 || bagdat[bag].gt != 0 ||
                    bagdat[bag].fallh != 0 || bagdat[bag].wobbling))
                {
                    bagdat[bag].Exists = false;
                    sprites.EraseSprite(bag + FIRSTBAG);
                }
                if (game.CurrentPlayer == 0)
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
                if (!bagdat[bag].Exists)
                    continue;

                if (bagdat[bag].gt != 0)
                {
                    if (bagdat[bag].gt == 1)
                    {
                        sound.soundbreak();
                        drawApi.DrawGold(bag, 4, bagdat[bag].x, bagdat[bag].y);
                        game.IncreasePenalty();
                    }
                    if (bagdat[bag].gt == 3)
                    {
                        drawApi.DrawGold(bag, 5, bagdat[bag].x, bagdat[bag].y);
                        game.IncreasePenalty();
                    }
                    if (bagdat[bag].gt == 5)
                    {
                        drawApi.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
                        game.IncreasePenalty();
                    }
                    bagdat[bag].gt++;
                    if (bagdat[bag].gt == goldtime)
                        RemoveBag(bag);
                    else
                      if (bagdat[bag].v < MHEIGHT - 1 && bagdat[bag].gt < goldtime - 10)
                        if ((monsters.GetField(bagdat[bag].h, bagdat[bag].v + 1) & 0x2000) == 0)
                            bagdat[bag].gt = goldtime - 10;
                }
                else
                    UpdateBag(bag);
            }

            for (int bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].dir == DIR_DOWN && bagdat[bag].Exists)
                    soundfalloffflag = false;
                if (bagdat[bag].dir != DIR_DOWN && bagdat[bag].wobbling && bagdat[bag].Exists)
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
                                game.IncreasePenalty();
                                sound.soundwobble();
                            }
                        }
                        else
                          if ((monsters.GetField(h, v + 1) & 0xfdf) != 0xfdf)
                            if (!diggers.checkdiggerunderbag(h, v + 1))
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
                        if (y < 180 && (monsters.GetField(h, v + 1) & 0xfdf) != 0xfdf)
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
                      if ((monsters.GetField(h, v + 1) & 0xfdf) == 0xfdf)
                        if (yr == 0)
                            OnBagHitsTheGround(bag);
                    monsters.CheckIsMonsterScared(bagdat[bag].h);
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
            game.IncreasePenalty();
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
                game.IncreasePenalty();
                i = clfirst[4];
                while (i != -1)
                {
                    if (diggers.DiggerY(i - FIRSTDIGGER + game.CurrentPlayer) >= y)
                        diggers.KillDigger(i - FIRSTDIGGER + game.CurrentPlayer, 1, bag);
                    i = clcoll[i];
                }
                if (clfirst[2] != -1)
                    monsters.SquashMonsters(bag, clfirst, clcoll);
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
                            drawApi.DrawSquareBlob(x, y);
                            drawApi.DrawTopBlob(x, y + 21);
                        }
                        else
                            drawApi.DrawFurryBlob(x, y);
                        drawApi.EatField(x, y, dir);
                        game.emeralds.KillEmerald(h, v);
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
                        game.IncreasePenalty();
                        i = clfirst[4];
                        while (i != -1)
                        {
                            if (diggers.DiggerY(i - FIRSTDIGGER + game.CurrentPlayer) >= y)
                                diggers.KillDigger(i - FIRSTDIGGER + game.CurrentPlayer, 1, bag);
                            i = clcoll[i];
                        }
                        if (clfirst[2] != -1)
                            monsters.SquashMonsters(bag, clfirst, clcoll);
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
                        game.IncreasePenalty();
                        pushcount = 1;
                        if (clfirst[1] != -1)
                            if (!PushBags(dir, clfirst, clcoll))
                            {
                                x = ox;
                                y = oy;
                                drawApi.DrawGold(bag, 0, ox, oy);
                                game.IncreasePenalty();
                                push = false;
                            }
                        i = clfirst[4];
                        digf = false;
                        while (i != -1)
                        {
                            if (diggers.IsDiggerAlive(i - FIRSTDIGGER + game.CurrentPlayer))
                                digf = true;
                            i = clcoll[i];
                        }
                        if (digf || clfirst[2] != -1)
                        {
                            x = ox;
                            y = oy;
                            drawApi.DrawGold(bag, 0, ox, oy);
                            game.IncreasePenalty();
                            push = false;
                        }
                        break;
                }
                if (push)
                    bagdat[bag].dir = dir;
                else
                    bagdat[bag].dir = diggers.reversedir(dir);
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
            if (bagdat[bag].Exists)
            {
                bagdat[bag].Exists = false;
                sprites.EraseSprite(bag + FIRSTBAG);
            }
        }

        public bool BagExists(int bag)
        {
            return bagdat[bag].Exists;
        }

        public int GetBagY(int bag)
        {
            return bagdat[bag].y;
        }

        public int GetBagDirection(int bag)
        {
            if (bagdat[bag].Exists)
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
                if (bagdat[bag].Exists && bagdat[bag].gt < 10 &&
                    (bagdat[bag].gt != 0 || bagdat[bag].wobbling))
                    n++;
            return n;
        }

        private void GetGold(int bag)
        {
            bool f = true;
            int i;
            drawApi.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
            game.IncreasePenalty();
            i = sprites.first[4];
            while (i != -1)
            {
                if (diggers.IsDiggerAlive(i - FIRSTDIGGER + game.CurrentPlayer))
                {
                    scores.scoregold(i - FIRSTDIGGER + game.CurrentPlayer);
                    sound.soundgold();
                    diggers.ResetDiggerTime(i - FIRSTDIGGER + game.CurrentPlayer);
                    f = false;
                }
                i = sprites.coll[i];
            }
            if (f)
                monsters.MonsterGotGold();

            RemoveBag(bag);
        }
    }
}