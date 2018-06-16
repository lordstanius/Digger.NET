/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System;

namespace Digger.Source
{
    public class Monsters
    {
        private struct MonsterData
        {
            public int x, y, h, v, xr, yr, dir, hdir, t, hnt, death, bag, dtime, stime, chase;
            public bool exists, isNobbin, isAlive;
        }

        private const int MONSTERS = Const.MONSTERS;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;
        private const int FIRSTBAG = Const.FIRSTBAG;

        private MonsterData[] mondat = new MonsterData[MONSTERS];

        private int nextMonster = 0;
        private int totalMonsters = 0;
        private int maxMonstersOnScreen = 0;
        private int nextMonsterTime = 0;
        private int monsterGapTime = 0;
        private int chase = 0;
        private bool monsterGotGold = false;
        private bool unBonusFlag = false;

        private readonly Game game;

        public Monsters(Game game)
        {
            this.game = game;
        }

        public void Init()
        {
            mondat = new MonsterData[MONSTERS];
            nextMonster = 0;
            monsterGapTime = 45 - (Level.LevelOf10(game.Level) << 1);
            totalMonsters = Level.LevelOf10(game.Level) + 5;
            switch (Level.LevelOf10(game.Level))
            {
                case 1:
                    maxMonstersOnScreen = 3;
                    break;
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                    maxMonstersOnScreen = 4;
                    break;
                case 8:
                case 9:
                case 10:
                    maxMonstersOnScreen = 5;
                    break;
            }
            nextMonsterTime = 10;
            unBonusFlag = true;
        }

        public void EraseMonsters()
        {
            for (int i = 0; i < MONSTERS; i++)
                if (mondat[i].exists)
                    game.sprite.EraseSprite(i + FIRSTMONSTER);
        }

        public void DoMonsters()
        {
            if (nextMonsterTime > 0)
            {
                nextMonsterTime--;
            }
            else
            {
                if (nextMonster < totalMonsters &&
                    MonstersOnScreenCount() < maxMonstersOnScreen &&
                    game.diggers.IsAnyAlive() &&
                    !game.diggers.isBonusMode)
                    CreateMonster();

                if (unBonusFlag && nextMonster == totalMonsters &&
                    nextMonsterTime == 0 && game.diggers.IsAnyAlive())
                {
                    unBonusFlag = false;
                    game.diggers.CreateBonus();
                }
            }

            for (int i = 0; i < MONSTERS; i++)
            {
                if (!mondat[i].exists)
                    continue;

                if (mondat[i].hnt > 10 - Level.LevelOf10(game.Level))
                {
                    if (mondat[i].isNobbin)
                    {
                        mondat[i].isNobbin = false;
                        mondat[i].hnt = 0;
                    }
                }

                if (mondat[i].isAlive)
                {
                    if (mondat[i].t == 0)
                    {
                        MonsterAI(i);
                        if (game.RandNo(15 - Level.LevelOf10(game.Level)) == 0) /* Need to split for determinism */
                            if (mondat[i].isNobbin && mondat[i].isAlive)
                                MonsterAI(i);
                    }
                    else
                        mondat[i].t--;
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
                if (!mondat[i].exists)
                {
                    mondat[i].exists = true;
                    mondat[i].isAlive = true;
                    mondat[i].isNobbin = true;
                    mondat[i].t = 0;
                    mondat[i].hnt = 0;
                    mondat[i].h = 14;
                    mondat[i].v = 0;
                    mondat[i].xr = 0;
                    mondat[i].yr = 0;
                    mondat[i].x = 292;
                    mondat[i].y = 18;
                    mondat[i].dir = Dir.Left;
                    mondat[i].hdir = Dir.Left;
                    mondat[i].chase = chase + game.currentPlayer;
                    chase = (chase + 1) % game.diggerCount;
                    nextMonster++;
                    nextMonsterTime = monsterGapTime;
                    mondat[i].stime = 5;
                    game.sprite.MoveDrawSprite(i + FIRSTMONSTER, mondat[i].x, mondat[i].y);
                    break;
                }
            }
        }

        public void MonsterGotGold()
        {
            monsterGotGold = true;
        }

        private void MonsterAI(int mon)
        {
            int monox, monoy, dir, mdirp1, mdirp2, mdirp3, mdirp4, t, i, m, dig;
            int[] clcoll = new int[SPRITES];
            int[] clfirst = new int[TYPES];
            bool push, bagf;
            monox = mondat[mon].x;
            monoy = mondat[mon].y;
            if (mondat[mon].xr == 0 && mondat[mon].yr == 0)
            {
                /* If we are here the monster needs to know which way to turn next. */

                /* Turn hobbin back into nobbin if it's had its time */

                if (mondat[mon].hnt > 30 + (Level.LevelOf10(game.Level) << 1))
                {
                    if (!mondat[mon].isNobbin)
                    {
                        mondat[mon].hnt = 0;
                        mondat[mon].isNobbin = true;
                    }
                }

                /* Set up monster direction properties to chase Digger */

                dig = mondat[mon].chase;
                if (!game.diggers.IsDiggerAlive(dig) && game.diggerCount > 1)
                    dig = dig == 0 ? 1 : 0;


                if (Math.Abs(game.diggers.DiggerY(dig) - mondat[mon].y) > Math.Abs(game.diggers.DiggerX(dig) - mondat[mon].x))
                {
                    if (game.diggers.DiggerY(dig) < mondat[mon].y) { mdirp1 = Dir.Up; mdirp4 = Dir.Down; }
                    else { mdirp1 = Dir.Down; mdirp4 = Dir.Up; }
                    if (game.diggers.DiggerX(dig) < mondat[mon].x) { mdirp2 = Dir.Left; mdirp3 = Dir.Right; }
                    else { mdirp2 = Dir.Right; mdirp3 = Dir.Left; }
                }
                else
                {
                    if (game.diggers.DiggerX(dig) < mondat[mon].x) { mdirp1 = Dir.Left; mdirp4 = Dir.Right; }
                    else { mdirp1 = Dir.Right; mdirp4 = Dir.Left; }
                    if (game.diggers.DiggerY(dig) < mondat[mon].y) { mdirp2 = Dir.Up; mdirp3 = Dir.Down; }
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

                if (game.RandNo(Level.LevelOf10(game.Level) + 5) == 1) /* Need to split for determinism */
                    if (Level.LevelOf10(game.Level) < 6)
                    {
                        t = mdirp1;
                        mdirp1 = mdirp3;
                        mdirp3 = t;
                    }

                /* Check field and find direction */

                if (ClearField(mdirp1, mondat[mon].h, mondat[mon].v))
                    dir = mdirp1;
                else if (ClearField(mdirp2, mondat[mon].h, mondat[mon].v))
                    dir = mdirp2;
                else if (ClearField(mdirp3, mondat[mon].h, mondat[mon].v))
                    dir = mdirp3;
                else if (ClearField(mdirp4, mondat[mon].h, mondat[mon].v))
                    dir = mdirp4;

                /* Hobbins don't care about the field: they go where they want. */

                if (!mondat[mon].isNobbin)
                    dir = mdirp1;

                /* Monsters take a time g_Penalty for changing direction */

                if (mondat[mon].dir != dir)
                    mondat[mon].t++;

                /* Save the new direction */

                mondat[mon].dir = dir;
            }

            /* If monster is about to go off edge of screen, stop it. */

            if ((mondat[mon].x == 292 && mondat[mon].dir == Dir.Right) ||
                (mondat[mon].x == 12 && mondat[mon].dir == Dir.Left) ||
                (mondat[mon].y == 180 && mondat[mon].dir == Dir.Down) ||
                (mondat[mon].y == 18 && mondat[mon].dir == Dir.Up))
                mondat[mon].dir = Dir.None;

            /* Change hdir for hobbin */

            if (mondat[mon].dir == Dir.Left || mondat[mon].dir == Dir.Right)
                mondat[mon].hdir = mondat[mon].dir;

            /* Hobbins dig */

            if (!mondat[mon].isNobbin)
                game.drawing.EatField(mondat[mon].x, mondat[mon].y, mondat[mon].dir);

            /* (Draw new tunnels) and move monster */

            switch (mondat[mon].dir)
            {
                case Dir.Right:
                    if (!mondat[mon].isNobbin)
                        game.drawing.DrawRightBlob(mondat[mon].x, mondat[mon].y);

                    mondat[mon].x += 4;
                    break;
                case Dir.Up:
                    if (!mondat[mon].isNobbin)
                        game.drawing.DrawTopBlob(mondat[mon].x, mondat[mon].y);

                    mondat[mon].y -= 3;
                    break;
                case Dir.Left:
                    if (!mondat[mon].isNobbin)
                        game.drawing.DrawLeftBlob(mondat[mon].x, mondat[mon].y);

                    mondat[mon].x -= 4;
                    break;
                case Dir.Down:
                    if (!mondat[mon].isNobbin)
                        game.drawing.DrawBottomBlob(mondat[mon].x, mondat[mon].y);

                    mondat[mon].y += 3;
                    break;
            }

            /* Hobbins can eat emeralds */

            if (!mondat[mon].isNobbin)
                game.emeralds.IsEmeraldHit((mondat[mon].x - 12) / 20, (mondat[mon].y - 18) / 18,
                (mondat[mon].x - 12) % 20, (mondat[mon].y - 18) % 18,
                    mondat[mon].dir);

            /* If Digger's gone, don't bother */

            if (!game.diggers.IsAnyAlive())
            {
                mondat[mon].x = monox;
                mondat[mon].y = monoy;
            }

            /* If monster's just started, don't move yet */

            if (mondat[mon].stime != 0)
            {
                mondat[mon].stime--;
                mondat[mon].x = monox;
                mondat[mon].y = monoy;
            }

            /* Increase time counter for hobbin */

            if (!mondat[mon].isNobbin && mondat[mon].hnt < 100)
                mondat[mon].hnt++;

            /* Draw monster */

            push = true;
            game.drawing.DrawMonster(mon, mondat[mon].isNobbin, mondat[mon].hdir, mondat[mon].x, mondat[mon].y);
            for (i = 0; i < TYPES; i++)
                clfirst[i] = game.sprite.first[i];
            for (i = 0; i < SPRITES; i++)
                clcoll[i] = game.sprite.coll[i];
            game.IncrementPenalty();

            /* Collision with another monster */

            if (clfirst[2] != -1)
            {
                mondat[mon].t++; /* Time g_Penalty */
                                 /* Ensure both aren't moving in the same dir. */
                i = clfirst[2];
                do
                {
                    m = i - FIRSTMONSTER;
                    if (mondat[mon].dir == mondat[m].dir && mondat[m].stime == 0 &&
                        mondat[mon].stime == 0)
                        mondat[m].dir = Dir.Reverse(mondat[m].dir);
                    /* The kludge here is to preserve playback for a bug in previous
                       versions. */
                    if (!game.recorder.kludge)
                        game.IncrementPenalty();
                    else
                        if ((m & 1) == 0)
                        game.IncrementPenalty();
                    i = clcoll[i];
                } while (i != -1);
                if (game.recorder.kludge)
                    if (clfirst[0] != -1)
                        game.IncrementPenalty();
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
                mondat[mon].t++; /* Time g_Penalty */
                monsterGotGold = false;
                if (mondat[mon].dir == Dir.Right || mondat[mon].dir == Dir.Left)
                {
                    push = game.bags.PushBags(mondat[mon].dir, clfirst, clcoll);      /* Horizontal push */
                    mondat[mon].t++; /* Time g_Penalty */
                }
                else
                    if (!game.bags.PushBagsUp(clfirst, clcoll)) /* Vertical push */
                    push = false;
                if (monsterGotGold) /* No time g_Penalty if monster eats gold */
                    mondat[mon].t = 0;
                if (!mondat[mon].isNobbin && mondat[mon].hnt > 1)
                    game.bags.RemoveBags(clfirst, clcoll); /* Hobbins eat bags */
            }

            /* Increase hobbin cross counter */

            if (mondat[mon].isNobbin && clfirst[2] != -1 && game.diggers.IsAnyAlive())
                mondat[mon].hnt++;

            /* See if bags push monster back */

            if (!push)
            {
                mondat[mon].x = monox;
                mondat[mon].y = monoy;
                game.drawing.DrawMonster(mon, mondat[mon].isNobbin, mondat[mon].hdir, mondat[mon].x, mondat[mon].y);
                game.IncrementPenalty();
                if (mondat[mon].isNobbin) /* The other way to create hobbin: stuck on h-bag */
                    mondat[mon].hnt++;
                if ((mondat[mon].dir == Dir.Up || mondat[mon].dir == Dir.Down) &&
                    mondat[mon].isNobbin)
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

            /* Update co-ordinates */

            mondat[mon].h = (mondat[mon].x - 12) / 20;
            mondat[mon].v = (mondat[mon].y - 18) / 18;
            mondat[mon].xr = (mondat[mon].x - 12) % 20;
            mondat[mon].yr = (mondat[mon].y - 18) % 18;
        }

        private void MonsterDie(int mon)
        {
            switch (mondat[mon].death)
            {
                case 1:
                    if (game.bags.GetBagY(mondat[mon].bag) + 6 > mondat[mon].y)
                        mondat[mon].y = (short)game.bags.GetBagY(mondat[mon].bag);

                    game.drawing.DrawMonsterDie(mon, mondat[mon].isNobbin, mondat[mon].hdir, mondat[mon].x, mondat[mon].y);
                    game.IncrementPenalty();
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

        private bool ClearField(int dir, int x, int y)
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

        public void CheckIsMonsterOnScreen(int h)
        {
            for (int i = 0; i < MONSTERS; i++)
                if (h == mondat[i].h && mondat[i].dir == Dir.Up)
                    mondat[i].dir = Dir.Down;
        }

        public void KillMonster(int mon)
        {
            if (!mondat[mon].exists)
                return;

            mondat[mon].exists = mondat[mon].isAlive = false;
            game.sprite.EraseSprite(mon + FIRSTMONSTER);
            if (game.diggers.isBonusMode)
                totalMonsters++;
        }

        public void SquashMonsters(int bag, int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2];

            while (next != -1)
            {
                int m = next - Const.FIRSTMONSTER;
                if (mondat[m].y >= game.bags.GetBagY(bag))
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
            mondat[mon].isAlive = false;
            mondat[mon].death = death;
            mondat[mon].bag = bag;
        }

        public int MonstersLeftCount()
        {
            return MonstersOnScreenCount() + totalMonsters - nextMonster;
        }

        private int MonstersOnScreenCount()
        {
            int n = 0;
            for (int i = 0; i < MONSTERS; i++)
                if (mondat[i].exists)
                    n++;

            return n;
        }

        public void IncreaseMonstersTime(int n)
        {
            if (n > MONSTERS)
                n = MONSTERS;

            for (int m = 1; m < n; m++)
                mondat[m].t++;
        }

        public int GetField(int x, int y)
        {
            return game.drawing.field[y * 15 + x];
        }
    }
}
