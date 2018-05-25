/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
using System;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public struct digger_struct
        {
            public int h, v, rx, ry, mdir, bagtime, rechargetime,
                  deathstage, deathbag, deathani, deathtime, emocttime, emn, msc, lives, ivt;
            public bool notfiring, firepressed, dead, levdone, invin;
            public digger_obj dob;
            public bullet_obj bob;
        }

        public static digger_struct[] digdat = new digger_struct[DIGGERS];

        public static int startbonustimeleft = 0, bonustimeleft;

        public static int emmask = 0;

        public static byte[] emfield = new byte[MSIZE];

        public static bool bonusvisible = false, bonusmode = false, digvisible;

        public static void initdigger()
        {
            for (int dig = g_CurrentPlayer; dig < g_Diggers + g_CurrentPlayer; dig++)
            {
                if (digdat[dig].lives == 0)
                    continue;

                digdat[dig].v = 9;
                digdat[dig].mdir = 4;
                digdat[dig].h = (g_Diggers == 1) ? 7 : (8 - dig * 2);
                int x = digdat[dig].h * 20 + 12;
                int dir = (dig == 0) ? DIR_RIGHT : DIR_LEFT;
                digdat[dig].rx = 0;
                digdat[dig].ry = 0;
                digdat[dig].bagtime = 0;
                digdat[dig].dead = false; /* alive !=> !dead but dead => !alive */
                digdat[dig].invin = false;
                digdat[dig].ivt = 0;
                digdat[dig].deathstage = 1;
                int y = digdat[dig].v * 18 + 18;
                digdat[dig].dob = new digger_obj(dig - g_CurrentPlayer, dir, x, y);
                digdat[dig].bob = new bullet_obj(dig - g_CurrentPlayer, dir, x, y);
                digdat[dig].dob.put();
                digdat[dig].notfiring = true;
                digdat[dig].emocttime = 0;
                digdat[dig].bob.expsn = 0;
                digdat[dig].firepressed = false;
                digdat[dig].rechargetime = 0;
                digdat[dig].emn = 0;
                digdat[dig].msc = 1;
            }
            digvisible = true;
            bonusvisible = bonusmode = false;
        }

        public static uint g_CurrentTime, g_FrameTime;

        public static void newframe()
        {
            if (g_isVideoSync)
            {
                for (; g_CurrentTime < g_FrameTime; g_CurrentTime += 17094)
                { /* 17094 = ticks in a refresh */
                    input.checkkeyb();
                }
                g_CurrentTime -= g_FrameTime;
            }
            else
            {
                timer.SyncFrame();
                input.checkkeyb();
                sdlGfx.UpdateScreen();
            }
        }

        public static uint cgtime;

        public static void drawdig(int n)
        {
            digdat[n].dob.animate();
            if (digdat[n].invin)
            {
                digdat[n].ivt--;
                if (digdat[n].ivt == 0)
                    digdat[n].invin = false;
                else
                  if (digdat[n].ivt % 10 < 5)
                    sprites.erasespr(FIRSTDIGGER + n - g_CurrentPlayer);
            }
        }

        public static void dodigger(SdlGraphics ddap)
        {
            newframe();
            if (g_isGauntletMode)
            {
                drawApi.drawlives(ddap);
                if (cgtime < g_FrameTime)
                    g_isTimeOut = true;
                cgtime -= g_FrameTime;
            }
            for (int n = g_CurrentPlayer; n < g_Diggers + g_CurrentPlayer; n++)
            {
                if (digdat[n].bob.expsn != 0)
                    drawexplosion(n);
                else
                    updatefire(ddap, n);
                if (digvisible)
                {
                    if (digdat[n].dob.alive)
                        if (digdat[n].bagtime != 0)
                        {
                            int tdir = digdat[n].dob.dir;
                            digdat[n].dob.dir = digdat[n].mdir;
                            drawdig(n);
                            digdat[n].dob.dir = tdir;
                            incpenalty();
                            digdat[n].bagtime--;
                        }
                        else
                            updatedigger(ddap, n);
                    else
                        diggerdie(ddap, n);
                }
                if (digdat[n].emocttime > 0)
                    digdat[n].emocttime--;
            }
            if (bonusmode && isalive())
            {
                if (bonustimeleft != 0)
                {
                    bonustimeleft--;
                    if (startbonustimeleft != 0 || bonustimeleft < 20)
                    {
                        startbonustimeleft--;
                        if ((bonustimeleft & 1) != 0)
                        {
                            ddap.SetIntensity(0);
                            soundbonus();
                        }
                        else
                        {
                            ddap.SetIntensity(1);
                            soundbonus();
                        }
                        if (startbonustimeleft == 0)
                        {
                            music(0);
                            soundbonusoff();
                            ddap.SetIntensity(1);
                        }
                    }
                }
                else
                {
                    endbonusmode(ddap);
                    soundbonusoff();
                    music(1);
                }
            }
            if (bonusmode && !isalive())
            {
                endbonusmode(ddap);
                soundbonusoff();
                music(1);
            }
        }

        private static void updatefire(SdlGraphics ddap, int n)
        {
            int pix, fx = 0, fy = 0;
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            int i;
            bool clflag;
            if (digdat[n].notfiring)
            {
                if (digdat[n].rechargetime != 0)
                {
                    digdat[n].rechargetime--;
                    if (digdat[n].rechargetime == 0)
                    {
                        digdat[n].dob.recharge();
                    }
                }
                else
                {
                    if (getfirepflag(n - g_CurrentPlayer))
                    {
                        if (digdat[n].dob.alive)
                        {
                            digdat[n].dob.discharge();
                            digdat[n].rechargetime = level.levof10() * 3 + 60;
                            digdat[n].notfiring = false;
                            switch (digdat[n].dob.dir)
                            {
                                case DIR_RIGHT:
                                    fx = digdat[n].dob.x + 8;
                                    fy = digdat[n].dob.y + 4;
                                    break;
                                case DIR_UP:
                                    fx = digdat[n].dob.x + 4;
                                    fy = digdat[n].dob.y;
                                    break;
                                case DIR_LEFT:
                                    fx = digdat[n].dob.x;
                                    fy = digdat[n].dob.y + 4;
                                    break;
                                case DIR_DOWN:
                                    fx = digdat[n].dob.x + 4;
                                    fy = digdat[n].dob.y + 8;
                                    break;
                                default:
                                    Environment.Exit(1);
                                    break;
                            }
                            digdat[n].bob.update(digdat[n].dob.dir, fx, fy);
                            digdat[n].bob.put();
                            soundfire(n);
                        }
                    }
                }
            }
            else
            {
                pix = 0;
                switch (digdat[n].bob.dir)
                {
                    case DIR_RIGHT:
                        digdat[n].bob.x += 8;
                        pix = ddap.GetPixel(digdat[n].bob.x, digdat[n].bob.y + 4) |
                            ddap.GetPixel(digdat[n].bob.x + 4, digdat[n].bob.y + 4);
                        break;
                    case DIR_UP:
                        digdat[n].bob.y -= 7;
                        pix = 0;
                        for (i = 0; i < 7; i++)
                            pix |= ddap.GetPixel(digdat[n].bob.x + 4, digdat[n].bob.y + i);
                        pix &= 0xc0;
                        break;
                    case DIR_LEFT:
                        digdat[n].bob.x -= 8;
                        pix = ddap.GetPixel(digdat[n].bob.x, digdat[n].bob.y + 4) |
                            ddap.GetPixel(digdat[n].bob.x + 4, digdat[n].bob.y + 4);
                        break;
                    case DIR_DOWN:
                        digdat[n].bob.y += 7;
                        pix = 0;
                        for (i = 0; i < 7; i++)
                            pix |= ddap.GetPixel(digdat[n].bob.x, digdat[n].bob.y + i);
                        pix &= 0x3;
                        break;
                }
                digdat[n].bob.animate();
                for (i = 0; i < TYPES; i++)
                    clfirst[i] = sprites.first[i];
                for (i = 0; i < SPRITES; i++)
                    clcoll[i] = sprites.coll[i];
                incpenalty();
                i = clfirst[2];
                while (i != -1)
                {
                    killmon(i - FIRSTMONSTER);
                    scores.scorekill(ddap, n);
                    digdat[n].bob.explode();
                    i = clcoll[i];
                }
                i = clfirst[4];
                while (i != -1)
                {
                    if (i - FIRSTDIGGER + g_CurrentPlayer != n && !digdat[i - FIRSTDIGGER + g_CurrentPlayer].invin
                        && digdat[i - FIRSTDIGGER + g_CurrentPlayer].dob.alive)
                    {
                        killdigger(i - FIRSTDIGGER + g_CurrentPlayer, 3, 0);
                        digdat[n].bob.explode();
                    }
                    i = clcoll[i];
                }
                if (clfirst[0] != -1 || clfirst[1] != -1 || clfirst[2] != -1 || clfirst[3] != -1 ||
                    clfirst[4] != -1)
                    clflag = true;
                else
                    clflag = false;
                if (clfirst[0] != -1 || clfirst[1] != -1 || clfirst[3] != -1)
                {
                    digdat[n].bob.explode();
                    i = clfirst[3];
                    while (i != -1)
                    {
                        if (digdat[i - FIRSTFIREBALL + g_CurrentPlayer].bob.expsn == 0)
                        {
                            digdat[i - FIRSTFIREBALL + g_CurrentPlayer].bob.explode();
                        }
                        i = clcoll[i];
                    }
                }
                switch (digdat[n].bob.dir)
                {
                    case DIR_RIGHT:
                        if (digdat[n].bob.x > 296)
                        {
                            digdat[n].bob.explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                digdat[n].bob.x -= 8;
                                digdat[n].bob.animate();
                                digdat[n].bob.explode();
                            }
                        }
                        break;
                    case DIR_UP:
                        if (digdat[n].bob.y < 15)
                        {
                            digdat[n].bob.explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                digdat[n].bob.y += 7;
                                digdat[n].bob.animate();
                                digdat[n].bob.explode();
                            }
                        }
                        break;
                    case DIR_LEFT:
                        if (digdat[n].bob.x < 16)
                        {
                            digdat[n].bob.explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                digdat[n].bob.x += 8;
                                digdat[n].bob.animate();
                                digdat[n].bob.explode();
                            }
                        }
                        break;
                    case DIR_DOWN:
                        if (digdat[n].bob.y > 183)
                        {
                            digdat[n].bob.explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                digdat[n].bob.y -= 7;
                                digdat[n].bob.animate();
                                digdat[n].bob.explode();
                            }
                        }
                        break;
                }
            }
        }

        public static void erasediggers()
        {
            int i;
            for (i = 0; i < g_Diggers; i++)
                sprites.erasespr(FIRSTDIGGER + i);

            digvisible = false;
        }

        public static void drawexplosion(int n)
        {
            if (digdat[n].bob.expsn < 4)
            {
                digdat[n].bob.animate();
                incpenalty();
            }
            else
            {
                killfire(n);
            }
        }

        public static void killfire(int n)
        {
            if (!digdat[n].notfiring)
            {
                digdat[n].notfiring = true;
                digdat[n].bob.remove();
            }
        }

        private static void updatedigger(SdlGraphics ddap, int n)
        {
            int dir, ddir, diggerox, diggeroy, nmon;
            bool push = true, bagf;
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            input.readdirect(n - g_CurrentPlayer);
            dir = input.getdirect(n - g_CurrentPlayer);
            if (dir == DIR_RIGHT || dir == DIR_UP || dir == DIR_LEFT || dir == DIR_DOWN)
                ddir = dir;
            else
                ddir = DIR_NONE;
            if (digdat[n].rx == 0 && (ddir == DIR_UP || ddir == DIR_DOWN))
                digdat[n].dob.dir = digdat[n].mdir = ddir;
            if (digdat[n].ry == 0 && (ddir == DIR_RIGHT || ddir == DIR_LEFT))
                digdat[n].dob.dir = digdat[n].mdir = ddir;
            if (dir == DIR_NONE)
                digdat[n].mdir = DIR_NONE;
            else
                digdat[n].mdir = digdat[n].dob.dir;
            if ((digdat[n].dob.x == 292 && digdat[n].mdir == DIR_RIGHT) ||
                (digdat[n].dob.x == 12 && digdat[n].mdir == DIR_LEFT) ||
                (digdat[n].dob.y == 180 && digdat[n].mdir == DIR_DOWN) ||
                (digdat[n].dob.y == 18 && digdat[n].mdir == DIR_UP))
                digdat[n].mdir = DIR_NONE;
            diggerox = digdat[n].dob.x;
            diggeroy = digdat[n].dob.y;
            if (digdat[n].mdir != DIR_NONE)
                drawApi.EatField(diggerox, diggeroy, digdat[n].mdir);
            switch (digdat[n].mdir)
            {
                case DIR_RIGHT:
                    drawApi.drawrightblob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.x += 4;
                    break;
                case DIR_UP:
                    drawApi.drawtopblob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.y -= 3;
                    break;
                case DIR_LEFT:
                    drawApi.drawleftblob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.x -= 4;
                    break;
                case DIR_DOWN:
                    drawApi.drawbottomblob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.y += 3;
                    break;
            }
            if (hitemerald((digdat[n].dob.x - 12) / 20, (digdat[n].dob.y - 18) / 18,
                           (digdat[n].dob.x - 12) % 20, (digdat[n].dob.y - 18) % 18,
                           digdat[n].mdir))
            {
                if (digdat[n].emocttime == 0)
                    digdat[n].emn = 0;
                scores.scoreemerald(ddap, n);
                soundem();
                soundemerald(digdat[n].emn);

                digdat[n].emn++;
                if (digdat[n].emn == 8)
                {
                    digdat[n].emn = 0;
                    scores.scoreoctave(ddap, n);
                }
                digdat[n].emocttime = 9;
            }
            drawdig(n);
            for (int i = 0; i < TYPES; i++)
                clfirst[i] = sprites.first[i];
            for (int i = 0; i < SPRITES; i++)
                clcoll[i] = sprites.coll[i];
            incpenalty();

            int j = clfirst[1];
            bagf = false;
            while (j != -1)
            {
                if (bagexist(j - FIRSTBAG))
                {
                    bagf = true;
                    break;
                }
                j = clcoll[j];
            }

            if (bagf)
            {
                if (digdat[n].mdir == DIR_RIGHT || digdat[n].mdir == DIR_LEFT)
                {
                    push = pushbags(ddap, digdat[n].mdir, clfirst, clcoll);
                    digdat[n].bagtime++;
                }
                else
                  if (!pushudbags(ddap, clfirst, clcoll))
                    push = false;
                if (!push)
                { /* Strange, push not completely defined */
                    digdat[n].dob.x = diggerox;
                    digdat[n].dob.y = diggeroy;
                    digdat[n].dob.dir = digdat[n].mdir;
                    drawdig(n);
                    incpenalty();
                    digdat[n].dob.dir = reversedir(digdat[n].mdir);
                }
            }
            if (clfirst[2] != -1 && bonusmode && digdat[n].dob.alive)
                for (nmon = killmonsters(clfirst, clcoll); nmon != 0; nmon--)
                {
                    soundeatm();
                    sceatm(ddap, n);
                }
            if (clfirst[0] != -1)
            {
                scores.scorebonus(ddap, n);
                initbonusmode(ddap);
            }
            digdat[n].h = (digdat[n].dob.x - 12) / 20;
            digdat[n].rx = (digdat[n].dob.x - 12) % 20;
            digdat[n].v = (digdat[n].dob.y - 18) / 18;
            digdat[n].ry = (digdat[n].dob.y - 18) % 18;
        }

        public static void sceatm(SdlGraphics ddap, int n)
        {
            scores.scoreeatm(ddap, n, digdat[n].msc);
            digdat[n].msc <<= 1;
        }

        public static int[] deatharc = { 3, 5, 6, 6, 5, 3, 0 };

        private static void diggerdie(SdlGraphics ddap, int n)
        {
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            bool alldead;
            switch (digdat[n].deathstage)
            {
                case 1:
                    if (bagy(digdat[n].deathbag) + 6 > digdat[n].dob.y)
                        digdat[n].dob.y = bagy(digdat[n].deathbag) + 6;
                    drawApi.drawdigger(n - g_CurrentPlayer, 15, digdat[n].dob.x, digdat[n].dob.y, false);
                    incpenalty();
                    if (getbagdir(digdat[n].deathbag) + 1 == 0)
                    {
                        soundddie();
                        digdat[n].deathtime = 5;
                        digdat[n].deathstage = 2;
                        digdat[n].deathani = 0;
                        digdat[n].dob.y -= 6;
                    }
                    break;
                case 2:
                    if (digdat[n].deathtime != 0)
                    {
                        digdat[n].deathtime--;
                        break;
                    }
                    if (digdat[n].deathani == 0)
                        music(2);
                    drawApi.drawdigger(n - g_CurrentPlayer, 14 - digdat[n].deathani, digdat[n].dob.x, digdat[n].dob.y,
                               false);
                    for (int i = 0; i < TYPES; i++)
                        clfirst[i] = sprites.first[i];
                    for (int i = 0; i < SPRITES; i++)
                        clcoll[i] = sprites.coll[i];
                    incpenalty();
                    if (digdat[n].deathani == 0 && clfirst[2] != -1)
                        killmonsters(clfirst, clcoll);
                    if (digdat[n].deathani < 4)
                    {
                        digdat[n].deathani++;
                        digdat[n].deathtime = 2;
                    }
                    else
                    {
                        digdat[n].deathstage = 4;
                        if (musicflag || g_Diggers > 1)
                            digdat[n].deathtime = 60;
                        else
                            digdat[n].deathtime = 10;
                    }
                    break;
                case 3:
                    digdat[n].deathstage = 5;
                    digdat[n].deathani = 0;
                    digdat[n].deathtime = 0;
                    break;
                case 5:
                    if (digdat[n].deathani >= 0 && digdat[n].deathani <= 6)
                    {
                        drawApi.drawdigger(n - g_CurrentPlayer, 15, digdat[n].dob.x,
                                   digdat[n].dob.y - deatharc[digdat[n].deathani], false);
                        if (digdat[n].deathani == 6 && !isalive())
                            musicoff();
                        incpenalty();
                        digdat[n].deathani++;
                        if (digdat[n].deathani == 1)
                            soundddie();
                        if (digdat[n].deathani == 7)
                        {
                            digdat[n].deathtime = 5;
                            digdat[n].deathani = 0;
                            digdat[n].deathstage = 2;
                        }
                    }
                    break;
                case 4:
                    if (digdat[n].deathtime != 0)
                        digdat[n].deathtime--;
                    else
                    {
                        digdat[n].dead = true;
                        alldead = true;
                        for (int i = 0; i < g_Diggers; i++)
                            if (!digdat[i].dead)
                            {
                                alldead = false;
                                break;
                            }
                        if (alldead)
                            setdead(true);
                        else if (isalive() && digdat[n].lives > 0)
                        {
                            if (!g_isGauntletMode)
                                digdat[n].lives--;
                            drawApi.drawlives(ddap);
                            if (digdat[n].lives > 0)
                            {
                                digdat[n].v = 9;
                                digdat[n].mdir = 4;
                                digdat[n].h = (g_Diggers == 1) ? 7 : (8 - n * 2);
                                digdat[n].dob.x = digdat[n].h * 20 + 12;
                                digdat[n].dob.dir = (n == 0) ? DIR_RIGHT : DIR_LEFT;
                                digdat[n].rx = 0;
                                digdat[n].ry = 0;
                                digdat[n].bagtime = 0;
                                digdat[n].dob.alive = true;
                                digdat[n].dead = false;
                                digdat[n].invin = true;
                                digdat[n].ivt = 50;
                                digdat[n].deathstage = 1;
                                digdat[n].dob.y = digdat[n].v * 18 + 18;
                                sprites.erasespr(n + FIRSTDIGGER - g_CurrentPlayer);
                                digdat[n].dob.put();
                                digdat[n].notfiring = true;
                                digdat[n].emocttime = 0;
                                digdat[n].firepressed = false;
                                digdat[n].bob.expsn = 0;
                                digdat[n].rechargetime = 0;
                                digdat[n].emn = 0;
                                digdat[n].msc = 1;
                            }
                            input.clearfire(n);
                            if (bonusmode)
                                music(0);
                            else
                                music(1);
                        }
                    }
                    break;
            }
        }

        private static void createbonus()
        {
            bonusvisible = true;
            drawApi.drawbonus(292, 18);
        }

        private static void initbonusmode(SdlGraphics ddap)
        {
            int i;
            bonusmode = true;
            erasebonus(ddap);
            ddap.SetIntensity(1);
            bonustimeleft = 250 - level.levof10() * 20;
            startbonustimeleft = 20;
            for (i = 0; i < g_Diggers; i++)
                digdat[i].msc = 1;
        }

        private static void endbonusmode(SdlGraphics ddap)
        {
            bonusmode = false;
            ddap.SetIntensity(0);
        }

        public static void erasebonus(SdlGraphics ddap)
        {
            if (bonusvisible)
            {
                bonusvisible = false;
                sprites.erasespr(FIRSTBONUS);
            }
            ddap.SetIntensity(0);
        }

        public static int reversedir(int dir)
        {
            switch (dir)
            {
                case DIR_RIGHT: return DIR_LEFT;
                case DIR_LEFT: return DIR_RIGHT;
                case DIR_UP: return DIR_DOWN;
                case DIR_DOWN: return DIR_UP;
            }
            return dir;
        }

        public static bool checkdiggerunderbag(int h, int v)
        {
            for (int n = g_CurrentPlayer; n < g_Diggers + g_CurrentPlayer; n++)
                if (digdat[n].dob.alive)
                    if (digdat[n].mdir == DIR_UP || digdat[n].mdir == DIR_DOWN)
                        if ((digdat[n].dob.x - 12) / 20 == h)
                            if ((digdat[n].dob.y - 18) / 18 == v || (digdat[n].dob.y - 18) / 18 + 1 == v)
                                return true;
            return false;
        }

        public static void killdigger(int n, int stage, int bag)
        {
            if (digdat[n].invin)
                return;
            if (digdat[n].deathstage < 2 || digdat[n].deathstage > 4)
            {
                digdat[n].dob.alive = false;
                digdat[n].deathstage = stage;
                digdat[n].deathbag = bag;
            }
        }

        public static void makeemfield()
        {
            emmask = (short)(1 << g_CurrentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (level.getlevch(x, y, level.levplan()) == 'C')
                        emfield[y * MWIDTH + x] |= (byte)emmask;
                    else
                        emfield[y * MWIDTH + x] &= (byte)~emmask;
        }

        public static void drawemeralds()
        {
            emmask = (short)(1 << g_CurrentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emfield[y * MWIDTH + x] & emmask) != 0)
                        drawApi.DrawEmerald(x * 20 + 12, y * 18 + 21);
        }

        static short[] embox = { 8, 12, 12, 9, 16, 12, 6, 9 };

        public static bool hitemerald(int x, int y, int rx, int ry, int dir)
        {
            bool hit = false;
            int r;
            if (dir != DIR_RIGHT && dir != DIR_UP && dir != DIR_LEFT && dir != DIR_DOWN)
                return hit;
            if (dir == DIR_RIGHT && rx != 0)
                x++;
            if (dir == DIR_DOWN && ry != 0)
                y++;
            if (dir == DIR_RIGHT || dir == DIR_LEFT)
                r = rx;
            else
                r = ry;
            if ((emfield[y * MWIDTH + x] & emmask) != 0)
            {
                if (r == embox[dir])
                {
                    drawApi.DrawEmerald(x * 20 + 12, y * 18 + 21);
                    incpenalty();
                }
                if (r == embox[dir + 1])
                {
                    drawApi.EraseEmerald(x * 20 + 12, y * 18 + 21);
                    incpenalty();
                    hit = true;
                    emfield[y * MWIDTH + x] &= (byte)~emmask;
                }
            }
            return hit;
        }

        public static int countem()
        {
            int n = 0;
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emfield[y * MWIDTH + x] & emmask) != 0)
                        n++;
            return n;
        }

        public static void killemerald(int x, int y)
        {
            if ((emfield[(y + 1) * MWIDTH + x] & emmask) != 0)
            {
                emfield[(y + 1) * MWIDTH + x] &= (byte)~emmask;
                drawApi.EraseEmerald(x * 20 + 12, (y + 1) * 18 + 21);
            }
        }

        static bool getfirepflag(int n)
        {
            return n == 0 ? input.firepflag : input.fire2pflag;
        }

        public static int diggerx(int n)
        {
            return digdat[n].dob.x;
        }

        public static int diggery(int n)
        {
            return digdat[n].dob.y;
        }

        public static bool digalive(int n)
        {
            return digdat[n].dob.alive;
        }

        public static void digresettime(int n)
        {
            digdat[n].bagtime = 0;
        }

        public static bool isalive()
        {
            int i;
            for (i = g_CurrentPlayer; i < g_Diggers + g_CurrentPlayer; i++)
                if (digdat[i].dob.alive)
                    return true;
            return false;
        }

        public static int getlives(int pl)
        {
            return digdat[pl].lives;
        }

        public static void addlife(int pl)
        {
            digdat[pl].lives++;
            sound1up();
        }

        public static void initlives()
        {
            int i;
            for (i = 0; i < g_Diggers + g_playerCount - 1; i++)
                digdat[i].lives = 3;
        }

        public static void declife(int pl)
        {
            if (!g_isGauntletMode)
                digdat[pl].lives--;
        }
    }
}