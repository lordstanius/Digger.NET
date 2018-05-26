/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public class Monsters
    {
        private struct monster
        {
            public int h, v, xr, yr, dir, time, hnt, death, bag, dtime, stime, chase;
            public bool Exists;
            public Monster monstrObj;
        }

        private const int MONSTERS = Const.MONSTERS;
        private const int DIR_NONE = Const.DIR_NONE;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;
        private const int FIRSTBAG = Const.FIRSTBAG;

        private monster[] mondat;

        private int nextmonster = 0;
        private int totalmonsters = 0;
        private int maxmononscr = 0;
        private int nextmontime = 0;
        private int mongaptime = 0;
        private int chase = 0;
        private bool mongotgold = false;
        private bool unbonusflag = false;

        private readonly Game game;
        private readonly DrawApi drawApi;

        public Monsters(Game game)
        {
            this.game = game;
            this.drawApi = game.drawApi;
        }

        public void Initialize()
        {
            mondat = new monster[MONSTERS];
            nextmonster = 0;
            mongaptime = 45 - (game.level.LevelOf10() << 1);
            totalmonsters = game.level.LevelOf10() + 5;
            switch (game.level.LevelOf10())
            {
                case 1:
                    maxmononscr = 3;
                    break;
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                    maxmononscr = 4;
                    break;
                case 8:
                case 9:
                case 10:
                    maxmononscr = 5;
                    break;
            }
            nextmontime = 10;
            unbonusflag = true;
        }

        public void EraseMonsters()
        {
            for (int i = 0; i < MONSTERS; i++)
                if (mondat[i].Exists)
                    game.sprites.EraseSprite(i + FIRSTMONSTER);
        }

        public void DoMonsters()
        {
            if (nextmontime > 0)
            {
                nextmontime--;
            }
            else
            {
                if (nextmonster < totalmonsters && MonstersOnScreenCount() < maxmononscr && game.diggers.IsAlive() && !game.diggers.bonusmode)
                    CreateMonster();

                if (unbonusflag && nextmonster == totalmonsters && nextmontime == 0 && game.diggers.IsAlive())
                {
                    unbonusflag = false;
                    game.diggers.createbonus();
                }
            }

            for (int i = 0; i < MONSTERS; i++)
            {
                if (!mondat[i].Exists)
                    continue;

                if (mondat[i].hnt > 10 - game.level.LevelOf10())
                {
                    if (mondat[i].monstrObj.IsNobbin)
                    {
                        mondat[i].monstrObj.Mutate();
                        mondat[i].hnt = 0;
                    }
                }

                if (mondat[i].monstrObj.IsAlive)
                {
                    if (mondat[i].time == 0)
                    {
                        MonsterAI(i);
                        if (StdLib.RandFrom0To(15 - game.level.LevelOf10()) == 0) /* Need to split for determinism */
                            if (!mondat[i].monstrObj.IsNobbin && mondat[i].monstrObj.IsAlive)
                                MonsterAI(i);
                    }
                    else
                        mondat[i].time--;
                }
                else
                {
                    MonsterDie(i);
                }
            }
        }

        private void CreateMonster()
        {
            for (int i = 0; i < MONSTERS; i++)
            {
                if (!mondat[i].Exists)
                {
                    mondat[i].Exists = true;
                    mondat[i].time = 0;
                    mondat[i].hnt = 0;
                    mondat[i].h = 14;
                    mondat[i].v = 0;
                    mondat[i].xr = 0;
                    mondat[i].yr = 0;
                    mondat[i].dir = DIR_LEFT;
                    mondat[i].chase = chase + game.CurrentPlayer;
                    mondat[i].monstrObj = new Monster(game, i, DIR_LEFT, 292, 18);
                    chase = (chase + 1) % game.DiggerCount;
                    nextmonster++;
                    nextmontime = mongaptime;
                    mondat[i].stime = 5;
                    mondat[i].monstrObj.Put();
                    break;
                }
            }
        }

        public void MonsterGotGold()
        {
            mongotgold = true;
        }

        private void MonsterAI(int mon)
        {
            int[] clcoll = new int[SPRITES];
            int[] clfirst = new int[TYPES];

            bool push, bagf, mopos_changed;

            Position mopos = mondat[mon].monstrObj.Position;
            short monox = mopos.x;
            short monoy = mopos.y;
            int dir, mdirp1, mdirp2, mdirp3, mdirp4, t, i;

            if (mondat[mon].xr == 0 && mondat[mon].yr == 0)
            {
                /* If we are here the monster needs to know which way to turn next. */

                /* Turn hobbin back into nobbin if it's had its time */

                if (mondat[mon].hnt > 30 + (game.level.LevelOf10() << 1))
                    if (!mondat[mon].monstrObj.IsNobbin)
                    {
                        mondat[mon].hnt = 0;
                        mondat[mon].monstrObj.Mutate();
                    }

                /* Set up monster direction properties to chase Digger */

                int dig = mondat[mon].chase;
                if (!game.diggers.IsDiggerAlive(dig))
                    dig = (game.DiggerCount - 1) - dig;

                if (System.Math.Abs(game.diggers.DiggerY(dig) - mopos.y) > System.Math.Abs(game.diggers.DiggerX(dig) - mopos.x))
                {
                    mdirp1 = 0;
                    if (game.diggers.DiggerY(dig) < mopos.y)
                    {
                        mdirp1 = DIR_UP;
                        mdirp4 = DIR_DOWN;
                    }
                    else { mdirp1 = DIR_DOWN; mdirp4 = DIR_UP; }
                    if (game.diggers.DiggerX(dig) < mopos.x) { mdirp2 = DIR_LEFT; mdirp3 = DIR_RIGHT; }
                    else { mdirp2 = DIR_RIGHT; mdirp3 = DIR_LEFT; }
                }
                else
                {
                    if (game.diggers.DiggerX(dig) < mopos.x) { mdirp1 = DIR_LEFT; mdirp4 = DIR_RIGHT; }
                    else { mdirp1 = DIR_RIGHT; mdirp4 = DIR_LEFT; }
                    if (game.diggers.DiggerY(dig) < mopos.y) { mdirp2 = DIR_UP; mdirp3 = DIR_DOWN; }
                    else { mdirp2 = DIR_DOWN; mdirp3 = DIR_UP; }
                }

                /* In bonus mode, run away from Digger */

                if (game.diggers.bonusmode)
                {
                    t = mdirp1; mdirp1 = mdirp4; mdirp4 = t;
                    t = mdirp2; mdirp2 = mdirp3; mdirp3 = t;
                }

                /* Adjust priorities so that monsters don't reverse direction unless they
                   really have to */

                dir = game.diggers.reversedir(mondat[mon].dir);
                if (dir == mdirp1)
                {
                    mdirp1 = mdirp2;
                    mdirp2 = mdirp3;
                    mdirp3 = mdirp4;
                    mdirp4 = dir;
                }
                if (dir == mdirp2)
                {
                    mdirp2 = mdirp3;
                    mdirp3 = mdirp4;
                    mdirp4 = dir;
                }
                if (dir == mdirp3)
                {
                    mdirp3 = mdirp4;
                    mdirp4 = dir;
                }

                /* Introduce a random element on levels <6 : occasionally swap p1 and p3 */

                if (StdLib.RandFrom0To(game.level.LevelOf10() + 5) == 1) /* Need to split for determinism */
                    if (game.level.LevelOf10() < 6)
                    {
                        t = mdirp1;
                        mdirp1 = mdirp3;
                        mdirp3 = t;
                    }

                /* Check field and find direction */

                if (ClearField(mdirp1, mondat[mon].h, mondat[mon].v))
                    dir = mdirp1;
                else
                  if (ClearField(mdirp2, mondat[mon].h, mondat[mon].v))
                    dir = mdirp2;
                else
                    if (ClearField(mdirp3, mondat[mon].h, mondat[mon].v))
                    dir = mdirp3;
                else
                      if (ClearField(mdirp4, mondat[mon].h, mondat[mon].v))
                    dir = mdirp4;

                /* Hobbins don't care about the field: they go where they want. */
                if (!mondat[mon].monstrObj.IsNobbin)
                    dir = mdirp1;

                /* Monsters take a time penalty for changing direction */

                if (mondat[mon].dir != dir)
                    mondat[mon].time++;

                /* Save the new direction */

                mondat[mon].dir = dir;
            }

            /* If monster is about to go off edge of screen, stop it. */

            if ((mopos.x == 292 && mondat[mon].dir == DIR_RIGHT) ||
                (mopos.x == 12 && mondat[mon].dir == DIR_LEFT) ||
                (mopos.y == 180 && mondat[mon].dir == DIR_DOWN) ||
                (mopos.y == 18 && mondat[mon].dir == DIR_UP))
                mondat[mon].dir = DIR_NONE;

            /* Change hdir for hobbin */

            if (mondat[mon].dir == DIR_LEFT || mondat[mon].dir == DIR_RIGHT)
            {
                mopos.dir = mondat[mon].dir;
                mondat[mon].monstrObj.Position = mopos;
            }

            /* Hobbins dig */

            if (!mondat[mon].monstrObj.IsNobbin)
                drawApi.EatField(mopos.x, mopos.y, mondat[mon].dir);

            /* (Draw new tunnels) and move monster */
            mopos_changed = true;
            switch (mondat[mon].dir)
            {
                case DIR_RIGHT:
                    if (!mondat[mon].monstrObj.IsNobbin)
                        drawApi.DrawRightBlob(mopos.x, mopos.y);
                    mopos.x += 4;
                    break;
                case DIR_UP:
                    if (!mondat[mon].monstrObj.IsNobbin)
                        drawApi.DrawTopBlob(mopos.x, mopos.y);
                    mopos.y -= 3;
                    break;
                case DIR_LEFT:
                    if (!mondat[mon].monstrObj.IsNobbin)
                        drawApi.DrawLeftBlob(mopos.x, mopos.y);
                    mopos.x -= 4;
                    break;
                case DIR_DOWN:
                    if (!mondat[mon].monstrObj.IsNobbin)
                        drawApi.DrawBottomBlob(mopos.x, mopos.y);
                    mopos.y += 3;
                    break;
                default:
                    mopos_changed = false;
                    break;
            }

            /* Hobbins can eat emeralds */
            if (!mondat[mon].monstrObj.IsNobbin)
                game.emeralds.HitEmerald((mopos.x - 12) / 20, (mopos.y - 18) / 18,
                           (mopos.x - 12) % 20, (mopos.y - 18) % 18,
                           mondat[mon].dir);

            /* If Digger's gone, don't bother */
            if (!game.diggers.IsAlive() && mopos_changed)
            {
                mopos.x = monox;
                mopos.y = monoy;
                mopos_changed = false;
            }

            /* If monster's just started, don't move yet */

            if (mondat[mon].stime != 0)
            {
                mondat[mon].stime--;
                if (mopos_changed)
                {
                    mopos.x = monox;
                    mopos.y = monoy;
                    mopos_changed = false;
                }
            }

            /* Increase time counter for hobbin */
            if (!mondat[mon].monstrObj.IsNobbin && mondat[mon].hnt < 100)

                mondat[mon].hnt++;

            if (mopos_changed)
            {
                mondat[mon].monstrObj.Position = mopos;
            }

            /* Draw monster */
            push = true;
            mondat[mon].monstrObj.Animate();
            for (i = 0; i < TYPES; i++)
                clfirst[i] = game.sprites.first[i];
            for (i = 0; i < SPRITES; i++)
                clcoll[i] = game.sprites.coll[i];
            game.IncreasePenalty();

            /* Collision with another monster */

            if (clfirst[2] != -1)
            {
                mondat[mon].time++; /* Time penalty */
                                      /* Ensure both aren't moving in the same dir. */
                i = clfirst[2];
                do
                {
                    int m = i - FIRSTMONSTER;
                    if (mondat[mon].dir == mondat[m].dir && mondat[m].stime == 0 &&
                        mondat[mon].stime == 0)
                        mondat[m].dir = game.diggers.reversedir(mondat[m].dir);
                    /* The kludge here is to preserve playback for a bug in previous
                       versions. */
                    if (!game.record.Kludge)
                        game.IncreasePenalty();
                    else
                      if ((m & 1) == 0)
                        game.IncreasePenalty();
                    i = clcoll[i];
                } while (i != -1);
                if (game.record.Kludge)
                    if (clfirst[0] != -1)
                        game.IncreasePenalty();
            }

            /* Check for collision with bag */

            i = clfirst[1];
            bagf = false;
            while (i != -1)
            {
                if (game.bags.BagExists(i - FIRSTBAG))
                {
                    bagf = true;
                    break;
                }
                i = clcoll[i];
            }

            if (bagf)
            {
                mondat[mon].time++; /* Time penalty */
                mongotgold = false;
                if (mondat[mon].dir == DIR_RIGHT || mondat[mon].dir == DIR_LEFT)
                {
                    push = game.bags.PushBags(mondat[mon].dir, clfirst, clcoll);      /* Horizontal push */
                    mondat[mon].time++; /* Time penalty */
                }
                else
                  if (!game.bags.PushBagsUp(clfirst, clcoll)) /* Vertical push */
                    push = false;
                if (mongotgold) /* No time penalty if monster eats gold */
                    mondat[mon].time = 0;
                if (!mondat[mon].monstrObj.IsNobbin && mondat[mon].hnt > 1)
                    game.bags.RemoveBags(clfirst, clcoll); /* Hobbins eat bags */
            }

            /* Increase hobbin cross counter */

            if (mondat[mon].monstrObj.IsNobbin && clfirst[2] != -1 && game.diggers.IsAlive())
                mondat[mon].hnt++;

            /* See if bags push monster back */

            if (!push)
            {
                if (mopos_changed)
                {
                    mopos.x = monox;
                    mopos.y = monoy;
                    mondat[mon].monstrObj.Position = mopos;
                    mopos_changed = false;
                }
                mondat[mon].monstrObj.Animate();
                game.IncreasePenalty();
                if (mondat[mon].monstrObj.IsNobbin) /* The other way to create hobbin: stuck on h-bag */
                    mondat[mon].hnt++;
                if ((mondat[mon].dir == DIR_UP || mondat[mon].dir == DIR_DOWN) &&
                    mondat[mon].monstrObj.IsNobbin)
                    mondat[mon].dir = game.diggers.reversedir(mondat[mon].dir); /* If vertical, give up */
            }

            /* Collision with Digger */

            if (clfirst[4] != -1 && game.diggers.IsAlive())
            {
                if (game.diggers.bonusmode)
                {
                    KillMonster(mon);
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (game.diggers.IsDiggerAlive(i - FIRSTDIGGER + game.CurrentPlayer))
                            game.diggers.sceatm(i - FIRSTDIGGER + game.CurrentPlayer, game.scores);
                        i = clcoll[i];
                    }
                    game.sound.soundeatm(); /* Collision in bonus mode */
                }
                else
                {
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (game.diggers.IsDiggerAlive(i - FIRSTDIGGER + game.CurrentPlayer))
                            game.diggers.KillDigger(i - FIRSTDIGGER + game.CurrentPlayer, 3, 0); /* Kill Digger */
                        i = clcoll[i];
                    }
                }
            }

            /* Update coordinates */
            mondat[mon].h = (mopos.x - 12) / 20;
            mondat[mon].v = (mopos.y - 18) / 18;
            mondat[mon].xr = (mopos.x - 12) % 20;
            mondat[mon].yr = (mopos.y - 18) % 18;
        }

        private void MonsterDie(int mon)
        {
            Position monpos;

            switch (mondat[mon].death)
            {
                case 1:
                    monpos = mondat[mon].monstrObj.Position;
                    if (game.bags.GetBagY(mondat[mon].bag) + 6 > monpos.y)
                    {
                        monpos.y = (short)game.bags.GetBagY(mondat[mon].bag);
                        mondat[mon].monstrObj.Position = monpos;
                    }
                    mondat[mon].monstrObj.Animate();
                    game.IncreasePenalty();
                    if (game.bags.GetBagDirection(mondat[mon].bag) == -1)
                    {
                        mondat[mon].dtime = 1;
                        mondat[mon].death = 4;
                    }
                    break;
                case 4:
                    if (mondat[mon].dtime != 0)
                        mondat[mon].dtime--;
                    else
                    {
                        KillMonster(mon);
                        if (game.DiggerCount == 2)
                            game.scores.scorekill2();
                        else
                            game.scores.scorekill(game.CurrentPlayer);
                    }
                    break;
            }
        }

        private bool ClearField(int dir, int x, int y)
        {
            switch (dir)
            {
                case DIR_RIGHT:
                    if (x < 14)
                        if ((GetField(x + 1, y) & 0x2000) == 0)
                            if ((GetField(x + 1, y) & 1) == 0 || (GetField(x, y) & 0x10) == 0)
                                return true;
                    break;
                case DIR_UP:
                    if (y > 0)
                        if ((GetField(x, y - 1) & 0x2000) == 0)
                            if ((GetField(x, y - 1) & 0x800) == 0 || (GetField(x, y) & 0x40) == 0)
                                return true;
                    break;
                case DIR_LEFT:
                    if (x > 0)
                        if ((GetField(x - 1, y) & 0x2000) == 0)
                            if ((GetField(x - 1, y) & 0x10) == 0 || (GetField(x, y) & 1) == 0)
                                return true;
                    break;
                case DIR_DOWN:
                    if (y < 9)
                        if ((GetField(x, y + 1) & 0x2000) == 0)
                            if ((GetField(x, y + 1) & 0x40) == 0 || (GetField(x, y) & 0x800) == 0)
                                return true;
                    break;
            }
            return false;
        }

        public void CheckIsMonsterScared(int h)
        {
            for (int i = 0; i < MONSTERS; i++)
                if (h == mondat[i].h && mondat[i].dir == DIR_UP)
                    mondat[i].dir = DIR_DOWN;
        }

        public void KillMonster(int mon)
        {
            if (!mondat[mon].Exists)
                return;

            mondat[mon].Exists = false;
            mondat[mon].monstrObj.Kill();
            if (game.diggers.bonusmode)
                totalmonsters++;
        }

        public void SquashMonsters(int bag, int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2];

            while (next != -1)
            {
                int m = next - Const.FIRSTMONSTER;
                Position monpos = mondat[m].monstrObj.Position;
                if (monpos.y >= game.bags.GetBagY(bag))
                    SquashMonster(m, 1, bag);
                next = clcoll[next];
            }
        }

        public int KillMonsters(int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2], m, n = 0;
            while (next != -1)
            {
                m = next - FIRSTMONSTER;
                KillMonster(m);
                n++;
                next = clcoll[next];
            }
            return n;
        }

        public void SquashMonster(int mon, int death, int bag)
        {
            mondat[mon].monstrObj.Damage();
            mondat[mon].death = death;
            mondat[mon].bag = bag;
        }

        public int MonstersLeftCount()
        {
            return MonstersOnScreenCount() + totalmonsters - nextmonster;
        }

        private int MonstersOnScreenCount()
        {
            int n = 0;
            for (int i = 0; i < MONSTERS; i++)
                if (mondat[i].Exists)
                    n++;

            return n;
        }

        public void IncreaseMonstersTime(int n)
        {
            if (n > MONSTERS)
                n = MONSTERS;

            for (int m = 1; m < n; m++)
                mondat[m].time++;
        }

        public int GetField(int x, int y)
        {
            return drawApi.field[y * 15 + x];
        }
    }
}
