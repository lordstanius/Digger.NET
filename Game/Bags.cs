/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

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
        private const int FIRSTBAG = Const.FIRSTBAG;
        private const int MAX_W = Const.MAX_W;
        private const int MAX_H = Const.MAX_H;
        private const int CHR_W = Const.CHR_W;
        private const int CHR_H = Const.CHR_H;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;

        private int[] wblanim = { 2, 0, 1, 0 };

        private readonly Game game;

        public Bags(Game game)
        {
            this.game = game;
        }

        public void Initialize()
        {
            short bag, x, y;
            pushcount = 0;
            goldtime = 150 - Level.LevelOf10(game.LevelNo) * 10;
            bagdat = new Bag[BAGS];
            for (bag = 0; bag < BAGS; bag++)
                bagdat[bag].Exists = false;

            bag = 0;
            for (x = 0; x < MWIDTH; x++)
            {
                for (y = 0; y < MHEIGHT; y++)
                {
                    if (Level.GetLevelChar(x, y, game.LevelNo, game.diggerCount) == 'B')
                    {
                        if (bag < BAGS)
                        {
                            bagdat[bag].Exists = true;
                            bagdat[bag].gt = 0;
                            bagdat[bag].fallh = 0;
                            bagdat[bag].dir = Dir.None;
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
                    }
                }
            }

            if (game.currentPlayer == 0)
                bagdat.CopyTo(bagdat1, 0);
            else
                bagdat.CopyTo(bagdat2, 0);
        }

        public void DrawBags()
        {
            short bag;
            for (bag = 0; bag < BAGS; bag++)
            {
                if (game.currentPlayer == 0)
                    bagdat[bag] = bagdat1[bag];
                else
                    bagdat[bag] = bagdat2[bag];

                if (bagdat[bag].Exists)
                    game.sprites.MoveDrawSprite(bag + FIRSTBAG, bagdat[bag].x, bagdat[bag].y);
            }
        }

        public void Cleanup()
        {
            short bag;
            game.sound.SoundFallOff();
            for (bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].Exists && ((bagdat[bag].h == 7 && bagdat[bag].v == 9) ||
                    bagdat[bag].xr != 0 || bagdat[bag].yr != 0 || bagdat[bag].gt != 0 ||
                    bagdat[bag].fallh != 0 || bagdat[bag].wobbling))
                {
                    bagdat[bag].Exists = false;
                    game.sprites.EraseSprite(bag + FIRSTBAG);
                }
                if (game.currentPlayer == 0)
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
                        game.sound.SoundBreak();
                        game.video.DrawGold(bag, 4, bagdat[bag].x, bagdat[bag].y);
                        game.IncreasePenalty();
                    }
                    if (bagdat[bag].gt == 3)
                    {
                        game.video.DrawGold(bag, 5, bagdat[bag].x, bagdat[bag].y);
                        game.IncreasePenalty();
                    }
                    if (bagdat[bag].gt == 5)
                    {
                        game.video.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
                        game.IncreasePenalty();
                    }
                    bagdat[bag].gt++;
                    if (bagdat[bag].gt == goldtime)
                        RemoveBag(bag);
                    else
                      if (bagdat[bag].v < MHEIGHT - 1 && bagdat[bag].gt < goldtime - 10)
                        if ((game.monsters.GetField(bagdat[bag].h, bagdat[bag].v + 1) & 0x2000) == 0)
                            bagdat[bag].gt = goldtime - 10;
                }
                else
                    UpdateBag(bag);
            }

            for (int bag = 0; bag < BAGS; bag++)
            {
                if (bagdat[bag].dir == Dir.Down && bagdat[bag].Exists)
                    soundfalloffflag = false;
                if (bagdat[bag].dir != Dir.Down && bagdat[bag].wobbling && bagdat[bag].Exists)
                    soundwobbleoffflag = false;
            }

            if (soundfalloffflag)
                game.sound.SoundFallOff();
            if (soundwobbleoffflag)
                game.sound.SoundWobbleOff();
        }

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
                case Dir.None:
                    if (y < 180 && xr == 0)
                    {
                        if (bagdat[bag].wobbling)
                        {
                            if (bagdat[bag].wt == 0)
                            {
                                bagdat[bag].dir = Dir.Down;
                                game.sound.SoundFall();
                                break;
                            }
                            bagdat[bag].wt--;
                            wbl = bagdat[bag].wt % 8;
                            if ((wbl & 1) == 0)
                            {
                                game.video.DrawGold(bag, wblanim[wbl >> 1], x, y);
                                game.IncreasePenalty();
                                game.sound.SoundWobble();
                            }
                        }
                        else
                          if ((game.monsters.GetField(h, v + 1) & 0xfdf) != 0xfdf)
                            if (!game.diggers.CheckIsDiggerUnderBag(h, v + 1))
                                bagdat[bag].wobbling = true;
                    }
                    else
                    {
                        bagdat[bag].wt = 15;
                        bagdat[bag].wobbling = false;
                    }
                    break;
                case Dir.Right:
                case Dir.Left:
                    if (xr == 0)
                    {
                        if (y < 180 && (game.monsters.GetField(h, v + 1) & 0xfdf) != 0xfdf)
                        {
                            bagdat[bag].dir = Dir.Down;
                            bagdat[bag].wt = 0;
                            game.sound.SoundFall();
                        }
                        else
                            OnBagHitsTheGround(bag);
                    }
                    break;
                case Dir.Down:
                    if (yr == 0)
                        bagdat[bag].fallh++;

                    if (y >= 180)
                        OnBagHitsTheGround(bag);
                    else
                      if ((game.monsters.GetField(h, v + 1) & 0xfdf) == 0xfdf)
                        if (yr == 0)
                            OnBagHitsTheGround(bag);

                    game.monsters.CheckIsMonsterScared(bagdat[bag].h);
                    break;
            }
            if (bagdat[bag].dir != Dir.None)
            {
                if (bagdat[bag].dir != Dir.Down && pushcount != 0)
                    pushcount--;
                else
                    PushBag(bag, bagdat[bag].dir);
            }
        }

        private void OnBagHitsTheGround(int bag)
        {
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            if (bagdat[bag].dir == Dir.Down && bagdat[bag].fallh > 1)
                bagdat[bag].gt = 1;
            else
                bagdat[bag].fallh = 0;

            bagdat[bag].dir = Dir.None;
            bagdat[bag].wt = 15;
            bagdat[bag].wobbling = false;
            game.video.DrawGold(bag, 0, bagdat[bag].x, bagdat[bag].y);
            for (int i = 0; i < TYPES; i++)
                clfirst[i] = game.sprites.first[i];

            for (int i = 0; i < SPRITES; i++)
                clcoll[i] = game.sprites.coll[i];

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

            if (bagdat[bag].dir == Dir.Down && (dir == Dir.Right || dir == Dir.Left))
            {
                game.video.DrawGold(bag, 3, x, y);
                for (i = 0; i < TYPES; i++)
                    clfirst[i] = game.sprites.first[i];

                for (i = 0; i < SPRITES; i++)
                    clcoll[i] = game.sprites.coll[i];

                game.IncreasePenalty();
                i = clfirst[4];
                while (i != -1)
                {
                    if (game.diggers.DiggerY(i - FIRSTDIGGER + game.currentPlayer) >= y)
                        game.diggers.KillDigger(i - FIRSTDIGGER + game.currentPlayer, 1, bag);
                    i = clcoll[i];
                }

                if (clfirst[2] != -1)
                    game.monsters.SquashMonsters(bag, clfirst, clcoll);

                return true;
            }

            if ((x == 292 && dir == Dir.Right) || (x == 12 && dir == Dir.Left) ||
                (y == 180 && dir == Dir.Down) || (y == 18 && dir == Dir.Up))
                push = false;

            if (push)
            {
                switch (dir)
                {
                    case Dir.Right:
                        x += 4;
                        break;
                    case Dir.Left:
                        x -= 4;
                        break;
                    case Dir.Down:
                        if (bagdat[bag].unfallen)
                        {
                            bagdat[bag].unfallen = false;
                            game.video.DrawSquareBlob(x, y);
                            game.video.DrawTopBlob(x, y + 21);
                        }
                        else
                            game.video.DrawFurryBlob(x, y);
                        game.video.EatField(x, y, dir);
                        game.emeralds.KillEmerald(h, v);
                        y += 6;
                        break;
                }
                switch (dir)
                {
                    case Dir.Down:
                        game.video.DrawGold(bag, 3, x, y);
                        for (i = 0; i < TYPES; i++)
                            clfirst[i] = game.sprites.first[i];
                        for (i = 0; i < SPRITES; i++)
                            clcoll[i] = game.sprites.coll[i];
                        game.IncreasePenalty();
                        i = clfirst[4];
                        while (i != -1)
                        {
                            if (game.diggers.DiggerY(i - FIRSTDIGGER + game.currentPlayer) >= y)
                                game.diggers.KillDigger(i - FIRSTDIGGER + game.currentPlayer, 1, bag);
                            i = clcoll[i];
                        }
                        if (clfirst[2] != -1)
                            game.monsters.SquashMonsters(bag, clfirst, clcoll);
                        break;
                    case Dir.Right:
                    case Dir.Left:
                        bagdat[bag].wt = 15;
                        bagdat[bag].wobbling = false;
                        game.video.DrawGold(bag, 0, x, y);
                        for (i = 0; i < TYPES; i++)
                            clfirst[i] = game.sprites.first[i];
                        for (i = 0; i < SPRITES; i++)
                            clcoll[i] = game.sprites.coll[i];
                        game.IncreasePenalty();
                        pushcount = 1;
                        if (clfirst[1] != -1)
                            if (!PushBags(dir, clfirst, clcoll))
                            {
                                x = ox;
                                y = oy;
                                game.video.DrawGold(bag, 0, ox, oy);
                                game.IncreasePenalty();
                                push = false;
                            }
                        i = clfirst[4];
                        digf = false;
                        while (i != -1)
                        {
                            if (game.diggers.IsDiggerAlive(i - FIRSTDIGGER + game.currentPlayer))
                                digf = true;
                            i = clcoll[i];
                        }
                        if (digf || clfirst[2] != -1)
                        {
                            x = ox;
                            y = oy;
                            game.video.DrawGold(bag, 0, ox, oy);
                            game.IncreasePenalty();
                            push = false;
                        }
                        break;
                }
                if (push)
                    bagdat[bag].dir = dir;
                else
                    bagdat[bag].dir = Dir.Reverse(dir);
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
                game.sprites.EraseSprite(bag + FIRSTBAG);
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
            game.video.DrawGold(bag, 6, bagdat[bag].x, bagdat[bag].y);
            game.IncreasePenalty();
            int i = game.sprites.first[4];
            bool f = true;
            while (i != -1)
            {
                if (game.diggers.IsDiggerAlive(i - FIRSTDIGGER + game.currentPlayer))
                {
                    game.scores.scoregold(i - FIRSTDIGGER + game.currentPlayer);
                    game.sound.SoundGold();
                    game.diggers.ResetDiggerTime(i - FIRSTDIGGER + game.currentPlayer);
                    f = false;
                }
                i = game.sprites.coll[i];
            }

            if (f)
                game.monsters.MonsterGotGold();

            RemoveBag(bag);
        }
    }
}