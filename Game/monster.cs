/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
using System;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public struct monster
        {
            public int h, v, xr, yr, dir, t, hnt, death, bag, dtime, stime, chase;
            public bool flag;
            public monster_obj mop;
        }

        public static monster[] mondat = new monster[MONSTERS];

        private static int nextmonster = 0, totalmonsters = 0, maxmononscr = 0, nextmontime = 0, mongaptime = 0;
        private static int chase = 0;

        private static bool unbonusflag = false;

        // static void createmonster();
        // static void monai(digger_draw_api *, short mon);
        // static void mondie(digger_draw_api *, short mon);
        // static bool fieldclear(short dir,short x,short y);
        // static void squashmonster(short mon,short death,short bag);
        // static short nmononscr();

        public static void initmonsters()
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

        public static void erasemonsters()
        {
            short i;
            for (i = 0; i < MONSTERS; i++)
                if (mondat[i].flag)
                    sprites.erasespr(i + FIRSTMONSTER);
        }

        public static void domonsters(SdlGraphics ddap)
        {
            short i;
            if (nextmontime > 0)
                nextmontime--;
            else
            {
                if (nextmonster < totalmonsters && nmononscr() < maxmononscr && isalive() && !bonusmode)
                    createmonster();

                if (unbonusflag && nextmonster == totalmonsters && nextmontime == 0 && isalive())
                {
                    unbonusflag = false;
                    createbonus();
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
                            monai(ddap, i);
                            if (randno(15 - level.levof10()) == 0) /* Need to split for determinism */
                                if (!mondat[i].mop.isnobbin() && mondat[i].mop.isalive())
                                    monai(ddap, i);
                        }
                        else
                            mondat[i].t--;
                    else
                        mondie(ddap, i);
                }
        }

        private static void createmonster()
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
                    mondat[i].chase = chase + g_CurrentPlayer;
                    mondat[i].mop = new monster_obj(i, MON_NOBBIN, DIR_LEFT, 292, 18);
                    chase = (chase + 1) % g_Diggers;
                    nextmonster++;
                    nextmontime = mongaptime;
                    mondat[i].stime = 5;
                    mondat[i].mop.put();
                    break;
                }
        }

        public static bool mongotgold = false;

        public static void mongold()
        {
            mongotgold = true;
        }

        private static void monai(SdlGraphics ddap, int mon)
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
                if (!digalive(dig))
                    dig = (g_Diggers - 1) - dig;

                if (Math.Abs(diggery(dig) - mopos.y) > Math.Abs(diggerx(dig) - mopos.x))
                {
                    mdirp1 = 0;
                    if (diggery(dig) < mopos.y)
                    {
                        mdirp1 = DIR_UP;
                        mdirp4 = DIR_DOWN;
                    }
                    else { mdirp1 = DIR_DOWN; mdirp4 = DIR_UP; }
                    if (diggerx(dig) < mopos.x) { mdirp2 = DIR_LEFT; mdirp3 = DIR_RIGHT; }
                    else { mdirp2 = DIR_RIGHT; mdirp3 = DIR_LEFT; }
                }
                else
                {
                    if (diggerx(dig) < mopos.x) { mdirp1 = DIR_LEFT; mdirp4 = DIR_RIGHT; }
                    else { mdirp1 = DIR_RIGHT; mdirp4 = DIR_LEFT; }
                    if (diggery(dig) < mopos.y) { mdirp2 = DIR_UP; mdirp3 = DIR_DOWN; }
                    else { mdirp2 = DIR_DOWN; mdirp3 = DIR_UP; }
                }

                /* In bonus mode, run away from Digger */

                if (bonusmode)
                {
                    t = mdirp1; mdirp1 = mdirp4; mdirp4 = t;
                    t = mdirp2; mdirp2 = mdirp3; mdirp3 = t;
                }

                /* Adjust priorities so that monsters don't reverse direction unless they
                   really have to */

                dir = reversedir(mondat[mon].dir);
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

                if (randno(level.levof10() + 5) == 1) /* Need to split for determinism */
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
                drawApi.eatfield(mopos.x, mopos.y, mondat[mon].dir);

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
                hitemerald((mopos.x - 12) / 20, (mopos.y - 18) / 18,
                           (mopos.x - 12) % 20, (mopos.y - 18) % 18,
                           mondat[mon].dir);

            /* If Digger's gone, don't bother */
            if (!isalive() && mopos_changed)
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
            incpenalty();

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
                        mondat[m].dir = reversedir(mondat[m].dir);
                    /* The kludge here is to preserve playback for a bug in previous
                       versions. */
                    if (!kludge)
                        incpenalty();
                    else
                      if ((m & 1) == 0)
                        incpenalty();
                    i = clcoll[i];
                } while (i != -1);
                if (kludge)
                    if (clfirst[0] != -1)
                        incpenalty();
            }

            /* Check for collision with bag */

            i = clfirst[1];
            bagf = false;
            while (i != -1)
            {
                if (bagexist(i - FIRSTBAG))
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
                    push = pushbags(ddap, mondat[mon].dir, clfirst, clcoll);      /* Horizontal push */
                    mondat[mon].t++; /* Time penalty */
                }
                else
                  if (!pushudbags(ddap, clfirst, clcoll)) /* Vertical push */
                    push = false;
                if (mongotgold) /* No time penalty if monster eats gold */
                    mondat[mon].t = 0;
                if (!mondat[mon].mop.isnobbin() && mondat[mon].hnt > 1)
                    removebags(clfirst, clcoll); /* Hobbins eat bags */
            }

            /* Increase hobbin cross counter */

            if (mondat[mon].mop.isnobbin() && clfirst[2] != -1 && isalive())
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
                incpenalty();
                if (mondat[mon].mop.isnobbin()) /* The other way to create hobbin: stuck on h-bag */
                    mondat[mon].hnt++;
                if ((mondat[mon].dir == DIR_UP || mondat[mon].dir == DIR_DOWN) &&
                    mondat[mon].mop.isnobbin())
                    mondat[mon].dir = reversedir(mondat[mon].dir); /* If vertical, give up */
            }

            /* Collision with Digger */

            if (clfirst[4] != -1 && isalive())
            {
                if (bonusmode)
                {
                    killmon(mon);
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (digalive(i - FIRSTDIGGER + g_CurrentPlayer))
                            sceatm(ddap, i - FIRSTDIGGER + g_CurrentPlayer);
                        i = clcoll[i];
                    }
                    soundeatm(); /* Collision in bonus mode */
                }
                else
                {
                    i = clfirst[4];
                    while (i != -1)
                    {
                        if (digalive(i - FIRSTDIGGER + g_CurrentPlayer))
                            killdigger(i - FIRSTDIGGER + g_CurrentPlayer, 3, 0); /* Kill Digger */
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

        private static void mondie(SdlGraphics ddap, int mon)
        {
            obj_position monpos;

            switch (mondat[mon].death)
            {
                case 1:
                    monpos = mondat[mon].mop.getpos();
                    if (bagy(mondat[mon].bag) + 6 > monpos.y)
                    {
                        monpos.y = bagy(mondat[mon].bag);
                        mondat[mon].mop.setpos(monpos);
                    }
                    mondat[mon].mop.animate();
                    incpenalty();
                    if (getbagdir(mondat[mon].bag) == -1)
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
                        killmon(mon);
                        if (g_Diggers == 2)
                            scorekill2(ddap);
                        else
                            scorekill(ddap, g_CurrentPlayer);
                    }
                    break;
            }
        }

        private static bool fieldclear(int dir, int x, int y)
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

        public static void checkmonscared(int h)
        {
            short m;
            for (m = 0; m < MONSTERS; m++)
                if (h == mondat[m].h && mondat[m].dir == DIR_UP)
                    mondat[m].dir = DIR_DOWN;
        }

        public static void killmon(int mon)
        {
            if (mondat[mon].flag)
            {
                mondat[mon].flag = false;
                mondat[mon].mop.kill();
                if (bonusmode)
                    totalmonsters++;
            }
        }

        public static void squashmonsters(int bag, int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2];


            while (next != -1)
            {
                int m = next - FIRSTMONSTER;
                obj_position monpos = mondat[m].mop.getpos();
                if (monpos.y >= bagy(bag))
                    squashmonster(m, 1, bag);
                next = clcoll[next];
            }
        }

        public static int killmonsters(int[] clfirst, int[] clcoll)
        {
            int next = clfirst[2], m, n = 0;
            while (next != -1)
            {
                m = next - FIRSTMONSTER;
                killmon(m);
                n++;
                next = clcoll[next];
            }
            return n;
        }

        public static void squashmonster(int mon, int death, int bag)
        {
            mondat[mon].mop.damage();
            mondat[mon].death = death;
            mondat[mon].bag = bag;
        }

        public static int monleft()
        {
            return nmononscr() + totalmonsters - nextmonster;
        }

        private static int nmononscr()
        {
            int n = 0;
            for (int i = 0; i < MONSTERS; i++)
                if (mondat[i].flag)
                    n++;
            return n;
        }

        public static void incmont(int n)
        {
            if (n > MONSTERS)
                n = MONSTERS;
            for (int m = 1; m < n; m++)
                mondat[m].t++;
        }

        public static int getfield(int x, int y)
        {
            return drawApi.field[y * 15 + x];
        }
    }
}
