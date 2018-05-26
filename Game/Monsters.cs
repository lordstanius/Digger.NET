/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
namespace Digger.Net
{
    public class Monsters
    {
        private struct monster
        {
            public int h, v, xr, yr, dir, t, hnt, death, bag, dtime, stime, chase;
            public bool flag;
            public monster_obj mop;
        }

        private const int MONSTERS = DiggerC.MONSTERS;
        private const int DIR_NONE = DiggerC.DIR_NONE;
        private const int DIR_RIGHT = DiggerC.DIR_RIGHT;
        private const int DIR_UP = DiggerC.DIR_UP;
        private const int DIR_LEFT = DiggerC.DIR_LEFT;
        private const int DIR_DOWN = DiggerC.DIR_DOWN;
        private const int TYPES = DiggerC.TYPES;
        private const int SPRITES = DiggerC.SPRITES;
        private const int FIRSTMONSTER = DiggerC.FIRSTMONSTER;
        private const int FIRSTDIGGER = DiggerC.FIRSTDIGGER;
        private const int FIRSTBAG = DiggerC.FIRSTBAG;
        // TODO: Refactor
        private const bool MON_NOBBIN = DiggerC.MON_NOBBIN;

        private monster[] mondat = new monster[MONSTERS];

        private int nextmonster = 0, totalmonsters = 0, maxmononscr = 0, nextmontime = 0, mongaptime = 0;
        private int chase = 0;

        private bool unbonusflag = false;
        private Level level;
        private Sprites sprites;
        private Sound sound;
        private DrawApi drawApi;
        private Record record;
        private Scores scores;

        public Monsters(Level level, Sprites sprites, Sound sound, DrawApi drawApi, Record record, Scores scores)
        {
            this.level = level;
            this.sprites = sprites;
            this.sound = sound;
            this.drawApi = drawApi;
            this.record = record;
            this.scores = scores;
        }

        public void Initialize()
        {
            for (int i = 0; i < MONSTERS; i++)
                mondat[i].flag = false;
            nextmonster = 0;
            mongaptime = 45 - (level.levof10() << 1);
            totalmonsters = level.levof10() + 5;
            switch (level.levof10())
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
            short i;
            for (i = 0; i < MONSTERS; i++)
                if (mondat[i].flag)
                    sprites.erasespr(i + FIRSTMONSTER);
        }

        public void domonsters(Bags bags, Digger digger)
        {
            short i;
            if (nextmontime > 0)
                nextmontime--;
            else
            {
                if (nextmonster < totalmonsters && nmononscr() < maxmononscr && digger.isalive() && !digger.bonusmode)
                    CreateMonster();

                if (unbonusflag && nextmonster == totalmonsters && nextmontime == 0 && digger.isalive())
                {
                    unbonusflag = false;
                    digger.createbonus();
                }
            }

            for (i = 0; i < MONSTERS; i++)
                if (mondat[i].flag)
                {
                    if (mondat[i].hnt > 10 - level.levof10())
                    {
                        if (mondat[i].mop.isnobbin())
                        {
                            mondat[i].mop.mutate();
                            mondat[i].hnt = 0;
                        }
                    }
                    if (mondat[i].mop.isalive())
                        if (mondat[i].t == 0)
                        {
                            monai(i, bags, digger);
                            if (StdLib.RandFrom0To(15 - level.levof10()) == 0) /* Need to split for determinism */
                                if (!mondat[i].mop.isnobbin() && mondat[i].mop.isalive())
                                    monai(i, bags, digger);
                        }
                        else
                            mondat[i].t--;
                    else
                        mondie(i, bags, digger);
                }
        }

        private void CreateMonster()
        {
            for (int i = 0; i < MONSTERS; i++)
                if (!mondat[i].flag)
                {
                    mondat[i].flag = true;
                    mondat[i].t = 0;
                    mondat[i].hnt = 0;
                    mondat[i].h = 14;
                    mondat[i].v = 0;
                    mondat[i].xr = 0;
                    mondat[i].yr = 0;
                    mondat[i].dir = DIR_LEFT;
                    mondat[i].chase = chase + DiggerC.g_CurrentPlayer;
                    mondat[i].mop = new monster_obj(i, MON_NOBBIN, DIR_LEFT, 292, 18);
                    chase = (chase + 1) % DiggerC.g_Diggers;
                    nextmonster++;
                    nextmontime = mongaptime;
                    mondat[i].stime = 5;
                    mondat[i].mop.put();
                    break;
                }
        }

        public bool mongotgold = false;

        public void mongold()
        {
            mongotgold = true;
        }

        private void monai(int mon, Bags bags, Digger digger)
        {
            int[] clcoll = new int[SPRITES];
            int[] clfirst = new int[TYPES];

            bool push, bagf, mopos_changed;

            obj_position mopos = mondat[mon].mop.getpos();
            int monox = mopos.x;
            int monoy = mopos.y;
            int dir, mdirp1, mdirp2, mdirp3, mdirp4, t, i;

            if (mondat[mon].xr == 0 && mondat[mon].yr == 0)
            {
                /* If we are here the monster needs to know which way to turn next. */

                /* Turn hobbin back into nobbin if it's had its time */

                if (mondat[mon].hnt > 30 + (level.levof10() << 1))
                    if (!mondat[mon].mop.isnobbin())
                    {
                        mondat[mon].hnt = 0;
                        mondat[mon].mop.mutate();
                    }

                /* Set up monster direction properties to chase Digger */

                int dig = mondat[mon].chase;
                if (!digger.digalive(dig))
                    dig = (DiggerC.g_Diggers - 1) - dig;

                if (System.Math.Abs(digger.diggery(dig) - mopos.y) > System.Math.Abs(digger.diggerx(dig) - mopos.x))
                {
                    mdirp1 = 0;
                    if (digger.diggery(dig) < mopos.y)
                    {
                        mdirp1 = DIR_UP;
                        mdirp4 = DIR_DOWN;
                    }
                    else { mdirp1 = DIR_DOWN; mdirp4 = DIR_UP; }
                    if (digger.diggerx(dig) < mopos.x) { mdirp2 = DIR_LEFT; mdirp3 = DIR_RIGHT; }
                    else { mdirp2 = DIR_RIGHT; mdirp3 = DIR_LEFT; }
                }
                else
                {
                    if (digger.diggerx(dig) < mopos.x) { mdirp1 = DIR_LEFT; mdirp4 = DIR_RIGHT; }
                    else { mdirp1 = DIR_RIGHT; mdirp4 = DIR_LEFT; }
                    if (digger.diggery(dig) < mopos.y) { mdirp2 = DIR_UP; mdirp3 = DIR_DOWN; }
                    else { mdirp2 = DIR_DOWN; mdirp3 = DIR_UP; }
                }

                /* In bonus mode, run away from Digger */

                if (digger.bonusmode)
                {
                    t = mdirp1; mdirp1 = mdirp4; mdirp4 = t;
                    t = mdirp2; mdirp2 = mdirp3; mdirp3 = t;
                }

                /* Adjust priorities so that monsters don't reverse direction unless they
                   really have to */

                dir = digger.reversedir(mondat[mon].dir);
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

                if (StdLib.RandFrom0To(level.levof10() + 5) == 1) /* Need to split for determinism */
                    if (level.levof10() < 6)
                    {
                        t = mdirp1;
                        mdirp1 = mdirp3;
                        mdirp3 = t;
                    }

                /* Check field and find direction */

                if (fieldclear(mdirp1, mondat[mon].h, mondat[mon].v))
                    dir = mdirp1;
                else
                  if (fieldclear(mdirp2, mondat[mon].h, mondat[mon].v))
                    dir = mdirp2;
                else
                    if (fieldclear(mdirp3, mondat[mon].h, mondat[mon].v))
                    dir = mdirp3;
                else
                      if (fieldclear(mdirp4, mondat[mon].h, mondat[mon].v))
                    dir = mdirp4;

                /* Hobbins don't care about the field: they go where they want. */
                if (!mondat[mon].mop.isnobbin())
                    dir = mdirp1;

                /* Monsters take a time penalty for changing direction */

                if (mondat[mon].dir != dir)
                    mondat[mon].t++;

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
                mondat[mon].mop.setpos(mopos);
            }

            /* Hobbins dig */

            if (!mondat[mon].mop.isnobbin())
                drawApi.EatField(mopos.x, mopos.y, mondat[mon].dir);

            /* (Draw new tunnels) and move monster */
            mopos_changed = true;
            switch (mondat[mon].dir)
            {
                case DIR_RIGHT:
                    if (!mondat[mon].mop.isnobbin())
                        drawApi.drawrightblob(mopos.x, mopos.y);
                    mopos.x += 4;
                    break;
                case DIR_UP:
                    if (!mondat[mon].mop.isnobbin())
                        drawApi.drawtopblob(mopos.x, mopos.y);
                    mopos.y -= 3;
                    break;
                case DIR_LEFT:
                    if (!mondat[mon].mop.isnobbin())
                        drawApi.drawleftblob(mopos.x, mopos.y);
                    mopos.x -= 4;
                    break;
                case DIR_DOWN:
                    if (!mondat[mon].mop.isnobbin())
                        drawApi.drawbottomblob(mopos.x, mopos.y);
                    mopos.y += 3;
                    break;
                default:
                    mopos_changed = false;
                    break;
            }

            /* Hobbins can eat emeralds */
            if (!mondat[mon].mop.isnobbin())
                DiggerC.hitemerald((mopos.x - 12) / 20, (mopos.y - 18) / 18,
                           (mopos.x - 12) % 20, (mopos.y - 18) % 18,
                           mondat[mon].dir);

            /* If Digger's gone, don't bother */
            if (!digger.isalive() && mopos_changed)
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
            if (!mondat[mon].mop.isnobbin() && mondat[mon].hnt < 100)

                mondat[mon].hnt++;

            if (mopos_changed)
            {
                mondat[mon].mop.setpos(mopos);
            }

            /* Draw monster */
            push = true;
            mondat[mon].mop.animate();
            for (i = 0; i < TYPES; i++)
                clfirst[i] = sprites.first[i];
            for (i = 0; i < SPRITES; i++)
                clcoll[i] = sprites.coll[i];
            DiggerC.incpenalty();

            /* Collision with another monster */

            if (clfirst[2] != -1)
            {
                mondat[mon].t++; /* Time penalty */
                                 /* Ensure both aren't moving in the same dir. */
                i = clfirst[2];
                do
                {
                    int m = i - FIRSTMONSTER;
                    if (mondat[mon].dir == mondat[m].dir && mondat[m].stime == 0 &&
                        mondat[mon].stime == 0)
                        mondat[m].dir = digger.reversedir(mondat[m].dir);
                    /* The kludge here is to preserve playback for a bug in previous
                       versions. */
                    if (!record.kludge)
                        DiggerC.incpenalty();
                    else
                      if ((m & 1) == 0)
                        DiggerC.incpenalty();
                    i = clcoll[i];
                } while (i != -1);
                if (record.kludge)
                    if (clfirst[0] != -1)
                        DiggerC.incpenalty();
            }

            /* Check for collision with bag */

            i = clfirst[1];
            bagf = false;
            while (i != -1)
            {
                if (bags.BagExists(i - FIRSTBAG))
                {
                    bagf = true;
                    break;
                }
                i = clcoll[i];
            }

            if (bagf)
            {
                mondat[mon].t++; /* Time penalty */
                mongotgold = false;
                if (mondat[mon].dir == DIR_RIGHT || mondat[mon].dir == DIR_LEFT)
                {
                    push = bags.PushBags(mondat[mon].dir, clfirst, clcoll);      /* Horizontal push */
                    mondat[mon].t++; /* Time penalty */
                }
                else
                  if (!bags.PushBagsUp(clfirst, clcoll)) /* Vertical push */
                    push = false;
                if (mongotgold) /* No time penalty if monster eats gold */
                    mondat[mon].t = 0;
                if (!mondat[mon].mop.isnobbin() && mondat[mon].hnt > 1)
                    bags.RemoveBags(clfirst, clcoll); /* Hobbins eat bags */
            }

            /* Increase hobbin cross counter */

            if (mondat[mon].mop.isnobbin() && clfirst[2] != -1 && digger.isalive())
                mondat[mon].hnt++;

            /* See if bags push monster back */

            if (!push)
            {
                if (mopos_changed)
                {
                    mopos.x = monox;
                    mopos.y = monoy;
                    mondat[mon].mop.setpos(mopos);
                    mopos_changed = false;
                }
                mondat[mon].mop.animate();
                DiggerC.incpenalty();
                if (mondat[mon].mop.isnobbin()) /* The other way to create hobbin: stuck on h-bag */
                    mondat[mon].hnt++;
                if ((mondat[mon].dir == DIR_UP || mondat[mon].dir == DIR_DOWN) &&
                    mondat[mon].mop.isnobbin())
                    mondat[mon].dir = digger.reversedir(mondat[mon].dir); /* If vertical, give up */
            }

            /* Collision with Digger */

            if (clfirst[4] != -1 && digger.isalive())
            {
                if (digger.bonusmode)
                {
                    killmon(mon, digger);
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (digger.digalive(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer))
                            digger.sceatm(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer, scores);
                        i = clcoll[i];
                    }
                    sound.soundeatm(); /* Collision in bonus mode */
                }
                else
                {
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (digger.digalive(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer))
                            digger.killdigger(i - FIRSTDIGGER + DiggerC.g_CurrentPlayer, 3, 0); /* Kill Digger */
                        i = clcoll[i];
                    }
                }
            }

            /* Update co-ordinates */

            mondat[mon].h = (mopos.x - 12) / 20;
            mondat[mon].v = (mopos.y - 18) / 18;
            mondat[mon].xr = (mopos.x - 12) % 20;
            mondat[mon].yr = (mopos.y - 18) % 18;
        }

        private void mondie(int mon, Bags bags, Digger digger)
        {
            obj_position monpos;

            switch (mondat[mon].death)
            {
                case 1:
                    monpos = mondat[mon].mop.getpos();
                    if (bags.BagY(mondat[mon].bag) + 6 > monpos.y)
                    {
                        monpos.y = bags.BagY(mondat[mon].bag);
                        mondat[mon].mop.setpos(monpos);
                    }
                    mondat[mon].mop.animate();
                    DiggerC.incpenalty();
                    if (bags.GetBagDir(mondat[mon].bag) == -1)
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
                        killmon(mon, digger);
                        if (DiggerC.g_Diggers == 2)
                            scores.scorekill2();
                        else
                            scores.scorekill(DiggerC.g_CurrentPlayer);
                    }
                    break;
            }
        }

        private bool fieldclear(int dir, int x, int y)
        {
            switch (dir)
            {
                case DIR_RIGHT:
                    if (x < 14)
                        if ((getfield(x + 1, y) & 0x2000) == 0)
                            if ((getfield(x + 1, y) & 1) == 0 || (getfield(x, y) & 0x10) == 0)
                                return true;
                    break;
                case DIR_UP:
                    if (y > 0)
                        if ((getfield(x, y - 1) & 0x2000) == 0)
                            if ((getfield(x, y - 1) & 0x800) == 0 || (getfield(x, y) & 0x40) == 0)
                                return true;
                    break;
                case DIR_LEFT:
                    if (x > 0)
                        if ((getfield(x - 1, y) & 0x2000) == 0)
                            if ((getfield(x - 1, y) & 0x10) == 0 || (getfield(x, y) & 1) == 0)
                                return true;
                    break;
                case DIR_DOWN:
                    if (y < 9)
                        if ((getfield(x, y + 1) & 0x2000) == 0)
                            if ((getfield(x, y + 1) & 0x40) == 0 || (getfield(x, y) & 0x800) == 0)
                                return true;
                    break;
            }
            return false;
        }

        public void checkmonscared(int h)
        {
            short m;
            for (m = 0; m < MONSTERS; m++)
                if (h == mondat[m].h && mondat[m].dir == DIR_UP)
                    mondat[m].dir = DIR_DOWN;
        }

        public void killmon(int mon, Digger digger)
        {
            if (mondat[mon].flag)
            {
                mondat[mon].flag = false;
                mondat[mon].mop.kill();
                if (digger.bonusmode)
                    totalmonsters++;
            }
        }

        public void SquashMonsters(Bags bags, int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2];

            while (next != -1)
            {
                int m = next - DiggerC.FIRSTMONSTER;
                obj_position monpos = mondat[m].mop.getpos();
                if (monpos.y >= bags.Current.y)
                    SquashMonster(m, 1, bags.Current.id);
                next = clcoll[next];
            }
        }

        public int KillMonsters(int[] clfirst, int[] clcoll, Digger digger)
        {
            int next = clfirst[2], m, n = 0;
            while (next != -1)
            {
                m = next - FIRSTMONSTER;
                killmon(m, digger);
                n++;
                next = clcoll[next];
            }
            return n;
        }

        public void SquashMonster(int mon, int death, int bag)
        {
            mondat[mon].mop.damage();
            mondat[mon].death = death;
            mondat[mon].bag = bag;
        }

        public int monleft()
        {
            return nmononscr() + totalmonsters - nextmonster;
        }

        private int nmononscr()
        {
            int n = 0;
            for (int i = 0; i < MONSTERS; i++)
                if (mondat[i].flag)
                    n++;
            return n;
        }

        public void incmont(int n)
        {
            if (n > MONSTERS)
                n = MONSTERS;
            for (int m = 1; m < n; m++)
                mondat[m].t++;
        }

        public int getfield(int x, int y)
        {
            return drawApi.field[y * 15 + x];
        }
    }
}
