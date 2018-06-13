/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

namespace Digger.Source
{
    public class Monsters
    {
        private struct monster
        {
            public int h, v, xr, yr, dir, time, hnt, death, bag, dtime, stime, chase;
            public bool Exists;
            public Monster monstr;
        }

        private const int MONSTERS = Const.MONSTERS;
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
        private readonly Drawing video;

        public Monsters(Game game)
        {
            this.game = game;
            this.video = game.drawing;
        }

        public void Init()
        {
            mondat = new monster[MONSTERS];
            nextmonster = 0;
            mongaptime = 45 - (Level.LevelOf10(game.LevelNo) << 1);
            totalmonsters = Level.LevelOf10(game.LevelNo) + 5;
            switch (Level.LevelOf10(game.LevelNo))
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
                if (nextmonster < totalmonsters && MonstersOnScreenCount() < maxmononscr && game.diggers.IsAnyAlive() && !game.diggers.isBonusMode)
                    CreateMonster();

                if (unbonusflag && nextmonster == totalmonsters && nextmontime == 0 && game.diggers.IsAnyAlive())
                {
                    unbonusflag = false;
                    game.diggers.CreateBonus();
                }
            }

            for (int i = 0; i < MONSTERS; i++)
            {
                if (!mondat[i].Exists)
                    continue;

                if (mondat[i].hnt > 10 - Level.LevelOf10(game.LevelNo))
                {
                    if (mondat[i].monstr.IsNobbin)
                    {
                        mondat[i].monstr.Mutate();
                        mondat[i].hnt = 0;
                    }
                }

                if (mondat[i].monstr.IsAlive)
                {
                    if (mondat[i].time == 0)
                    {
                        MonsterAI(i);
                        if (game.RandNo(15 - Level.LevelOf10(game.LevelNo)) == 0) /* Need to split for determinism */
                            if (mondat[i].monstr.IsNobbin && mondat[i].monstr.IsAlive)
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
                    mondat[i].dir = Dir.Left;
                    mondat[i].chase = chase + game.currentPlayer;
                    mondat[i].monstr = new Monster(game, i, Dir.Left, 292, 18);
                    chase = (chase + 1) % game.diggerCount;
                    nextmonster++;
                    nextmontime = mongaptime;
                    mondat[i].stime = 5;
                    mondat[i].monstr.Put();
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

            Position mopos = mondat[mon].monstr.Position;
            short monox = mopos.x;
            short monoy = mopos.y;
            int dir, mdirp1, mdirp2, mdirp3, mdirp4, t, i;

            if (mondat[mon].xr == 0 && mondat[mon].yr == 0)
            {
                /* If we are here the monster needs to know which way to turn next. */

                /* Turn hobbin back into nobbin if it's had its time */

                if (mondat[mon].hnt > 30 + (Level.LevelOf10(game.LevelNo) << 1))
                {
                    if (!mondat[mon].monstr.IsNobbin)
                    {
                        mondat[mon].hnt = 0;
                        mondat[mon].monstr.Mutate();
                    }
                }

                /* Set up monster direction properties to chase Digger */

                int dig = mondat[mon].chase;
                if (!game.diggers.IsDiggerAlive(dig) && game.diggerCount > 1)
                    dig = dig == 1 ? 0 : 1; // chase the other if one is dead

                if (System.Math.Abs(game.diggers.DiggerY(dig) - mopos.y) > System.Math.Abs(game.diggers.DiggerX(dig) - mopos.x))
                {
                    if (game.diggers.DiggerY(dig) < mopos.y) { mdirp1 = Dir.Up; mdirp4 = Dir.Down; }
                    else { mdirp1 = Dir.Down; mdirp4 = Dir.Up; }
                    if (game.diggers.DiggerX(dig) < mopos.x) { mdirp2 = Dir.Left; mdirp3 = Dir.Right; }
                    else { mdirp2 = Dir.Right; mdirp3 = Dir.Left; }
                }
                else
                {
                    if (game.diggers.DiggerX(dig) < mopos.x) { mdirp1 = Dir.Left; mdirp4 = Dir.Right; }
                    else { mdirp1 = Dir.Right; mdirp4 = Dir.Left; }
                    if (game.diggers.DiggerY(dig) < mopos.y) { mdirp2 = Dir.Up; mdirp3 = Dir.Down; }
                    else { mdirp2 = Dir.Down; mdirp3 = Dir.Up; }
                }

                /* In bonus mode, run away from Digger */

                if (game.diggers.isBonusMode)
                {
                    t = mdirp1; mdirp1 = mdirp4; mdirp4 = t;
                    t = mdirp2; mdirp2 = mdirp3; mdirp3 = t;
                }

                /* Adjust priorities so that monsters don't reverse direction unless they
                   really have to */

                dir = Dir.Reverse(mondat[mon].dir);
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

                if (game.RandNo(Level.LevelOf10(game.LevelNo) + 5) == 1) /* Need to split for determinism */
                    if (Level.LevelOf10(game.LevelNo) < 6)
                    {
                        t = mdirp1;
                        mdirp1 = mdirp3;
                        mdirp3 = t;
                    }

                /* Check field and find direction */

                if (FieldClear(mdirp1, mondat[mon].h, mondat[mon].v))
                    dir = mdirp1;
                else
                  if (FieldClear(mdirp2, mondat[mon].h, mondat[mon].v))
                    dir = mdirp2;
                else
                    if (FieldClear(mdirp3, mondat[mon].h, mondat[mon].v))
                    dir = mdirp3;
                else
                      if (FieldClear(mdirp4, mondat[mon].h, mondat[mon].v))
                    dir = mdirp4;

                /* Hobbins don't care about the field: they go where they want. */
                if (!mondat[mon].monstr.IsNobbin)
                    dir = mdirp1;

                /* Monsters take a time penalty for changing direction */

                if (mondat[mon].dir != dir)
                    mondat[mon].time++;

                /* Save the new direction */

                mondat[mon].dir = dir;
            }

            /* If monster is about to go off edge of screen, stop it. */

            if ((mopos.x == 292 && mondat[mon].dir == Dir.Right) ||
                (mopos.x == 12 && mondat[mon].dir == Dir.Left) ||
                (mopos.y == 180 && mondat[mon].dir == Dir.Down) ||
                (mopos.y == 18 && mondat[mon].dir == Dir.Up))
                mondat[mon].dir = Dir.None;

            /* Change hdir for hobbin */

            if (mondat[mon].dir == Dir.Left || mondat[mon].dir == Dir.Right)
            {
                mopos.dir = mondat[mon].dir;
                mondat[mon].monstr.Position = mopos;
            }

            /* Hobbins dig */

            if (!mondat[mon].monstr.IsNobbin)
                video.EatField(mopos.x, mopos.y, mondat[mon].dir);

            /* (Draw new tunnels) and move monster */
            mopos_changed = true;
            switch (mondat[mon].dir)
            {
                case Dir.Right:
                    if (!mondat[mon].monstr.IsNobbin)
                        video.DrawRightBlob(mopos.x, mopos.y);
                    mopos.x += 4;
                    break;
                case Dir.Up:
                    if (!mondat[mon].monstr.IsNobbin)
                        video.DrawTopBlob(mopos.x, mopos.y);
                    mopos.y -= 3;
                    break;
                case Dir.Left:
                    if (!mondat[mon].monstr.IsNobbin)
                        video.DrawLeftBlob(mopos.x, mopos.y);
                    mopos.x -= 4;
                    break;
                case Dir.Down:
                    if (!mondat[mon].monstr.IsNobbin)
                        video.DrawBottomBlob(mopos.x, mopos.y);
                    mopos.y += 3;
                    break;
                default:
                    mopos_changed = false;
                    break;
            }

            /* Hobbins can eat emeralds */
            if (!mondat[mon].monstr.IsNobbin)
                game.emeralds.HitEmerald((mopos.x - 12) / 20, (mopos.y - 18) / 18, (mopos.x - 12) % 20, (mopos.y - 18) % 18, mondat[mon].dir);

            /* If Digger's gone, don't bother */
            if (!game.diggers.IsAnyAlive() && mopos_changed)
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
            if (!mondat[mon].monstr.IsNobbin && mondat[mon].hnt < 100)

                mondat[mon].hnt++;

            if (mopos_changed)
            {
                mondat[mon].monstr.Position = mopos;
            }

            /* Draw monster */
            push = true;
            mondat[mon].monstr.Animate();
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
                        mondat[m].dir = Dir.Reverse(mondat[m].dir);
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
                if (mondat[mon].dir == Dir.Right || mondat[mon].dir == Dir.Left)
                {
                    push = game.bags.PushBags(mondat[mon].dir, clfirst, clcoll);      /* Horizontal push */
                    mondat[mon].time++; /* Time penalty */
                }
                else if (!game.bags.PushBagsUp(clfirst, clcoll)) /* Vertical push */
                    push = false;
                if (mongotgold) /* No time penalty if monster eats gold */
                    mondat[mon].time = 0;
                if (!mondat[mon].monstr.IsNobbin && mondat[mon].hnt > 1)
                    game.bags.RemoveBags(clfirst, clcoll); /* Hobbins eat bags */
            }

            /* Increase hobbin cross counter */

            if (mondat[mon].monstr.IsNobbin && clfirst[2] != -1 && game.diggers.IsAnyAlive())
                mondat[mon].hnt++;

            /* See if bags push monster back */

            if (!push)
            {
                if (mopos_changed)
                {
                    mopos.x = monox;
                    mopos.y = monoy;
                    mondat[mon].monstr.Position = mopos;
                    mopos_changed = false;
                }
                mondat[mon].monstr.Animate();
                game.IncreasePenalty();
                if (mondat[mon].monstr.IsNobbin) /* The other way to create hobbin: stuck on h-bag */
                    mondat[mon].hnt++;
                if ((mondat[mon].dir == Dir.Up || mondat[mon].dir == Dir.Down) &&
                    mondat[mon].monstr.IsNobbin)
                    mondat[mon].dir = Dir.Reverse(mondat[mon].dir); /* If vertical, give up */
            }

            /* Collision with Digger */

            if (clfirst[4] != -1 && game.diggers.IsAnyAlive())
            {
                if (game.diggers.isBonusMode)
                {
                    KillMonster(mon);
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (game.diggers.IsDiggerAlive(i - FIRSTDIGGER + game.currentPlayer))
                            game.diggers.ScoreEatMonster(i - FIRSTDIGGER + game.currentPlayer);
                        i = clcoll[i];
                    }
                    game.sound.SoundEatMonster(); /* Collision in bonus mode */
                }
                else
                {
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (game.diggers.IsDiggerAlive(i - FIRSTDIGGER + game.currentPlayer))
                            game.diggers.KillDigger(i - FIRSTDIGGER + game.currentPlayer, 3, 0); /* Kill Digger */
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
                    monpos = mondat[mon].monstr.Position;
                    if (game.bags.GetBagY(mondat[mon].bag) + 6 > monpos.y)
                    {
                        monpos.y = (short)game.bags.GetBagY(mondat[mon].bag);
                        mondat[mon].monstr.Position = monpos;
                    }
                    mondat[mon].monstr.Animate();
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
                        if (game.diggerCount == 2)
                            game.scores.ScoreKill();
                        else
                            game.scores.ScoreKill(game.currentPlayer);
                    }
                    break;
            }
        }

        private bool FieldClear(int dir, int x, int y)
        {
            switch (dir)
            {
                case Dir.Right:
                    if (x < 14)
                        if ((GetField(x + 1, y) & 0x2000) == 0)
                            if ((GetField(x + 1, y) & 1) == 0 || (GetField(x, y) & 0x10) == 0)
                                return true;
                    break;
                case Dir.Up:
                    if (y > 0)
                        if ((GetField(x, y - 1) & 0x2000) == 0)
                            if ((GetField(x, y - 1) & 0x800) == 0 || (GetField(x, y) & 0x40) == 0)
                                return true;
                    break;
                case Dir.Left:
                    if (x > 0)
                        if ((GetField(x - 1, y) & 0x2000) == 0)
                            if ((GetField(x - 1, y) & 0x10) == 0 || (GetField(x, y) & 1) == 0)
                                return true;
                    break;
                case Dir.Down:
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
                if (h == mondat[i].h && mondat[i].dir == Dir.Up)
                    mondat[i].dir = Dir.Down;
        }

        public void KillMonster(int mon)
        {
            if (!mondat[mon].Exists)
                return;

            mondat[mon].Exists = false;
            mondat[mon].monstr.Kill();
            if (game.diggers.isBonusMode)
                totalmonsters++;
        }

        public void SquashMonsters(int bag, int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2];

            while (next != -1)
            {
                int m = next - Const.FIRSTMONSTER;
                Position monpos = mondat[m].monstr.Position;
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
            mondat[mon].monstr.Damage();
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
            return video.field[y * 15 + x];
        }
    }
}