/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
using System;

namespace Digger.Net
{
    public struct digger_struct
    {
        public int h, v, rx, ry, mdir, bagtime, rechargetime,
              deathstage, deathbag, deathani, deathtime, emocttime, emn, msc, lives, ivt;
        public bool notfiring, firepressed, dead, levdone, invin;
        public Digger dob;
        public Bullet bob;
    }

    public class Diggers
    {
        private const int MONSTERS = Const.MONSTERS;
        private const int DIR_NONE = Const.DIR_NONE;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int DIGGERS = Const.DIGGERS;
        private const int MSIZE = Const.MSIZE;
        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int FIRSTFIREBALL = Const.FIRSTFIREBALL;

        public digger_struct[] digdat = new digger_struct[DIGGERS];

        public int startbonustimeleft = 0;
        public int bonustimeleft;

        public bool bonusvisible = false;
        public bool bonusmode = false;
        public bool digvisible;
        public uint cgtime;

        private readonly Game game;
        private readonly Video video;
        private readonly Input input;
        private readonly Sound sound;
        private readonly Sprites sprites;
        private readonly Level level;

        public Diggers(Game game)
        {
            this.game = game;
            this.input = game.input;
            this.video = game.video;
            this.sound = game.sound;
            this.sprites = game.sprites;
            this.level = game.level;
        }

        public void InitializeDiggers()
        {
            for (int dig = game.CurrentPlayer; dig < game.DiggerCount + game.CurrentPlayer; dig++)
            {
                if (digdat[dig].lives == 0)
                    continue;

                digdat[dig].v = 9;
                digdat[dig].mdir = 4;
                digdat[dig].h = (game.DiggerCount == 1) ? 7 : (8 - dig * 2);
                int x = digdat[dig].h * 20 + 12;
                int dir = (dig == 0) ? DIR_RIGHT : DIR_LEFT;
                int y = digdat[dig].v * 18 + 18;
                digdat[dig].rx = 0;
                digdat[dig].ry = 0;
                digdat[dig].bagtime = 0;
                digdat[dig].dead = false; /* alive !=> !dead but dead => !alive */
                digdat[dig].invin = false;
                digdat[dig].ivt = 0;
                digdat[dig].deathstage = 1;
                digdat[dig].dob = new Digger(game, dig - game.CurrentPlayer, dir, x, y);
                digdat[dig].bob = new Bullet(game, dig - game.CurrentPlayer, dir, x, y);
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

        public void DrawDigger(int n)
        {
            digdat[n].dob.animate();
            if (digdat[n].invin)
            {
                digdat[n].ivt--;
                if (digdat[n].ivt == 0)
                    digdat[n].invin = false;
                else
                  if (digdat[n].ivt % 10 < 5)
                    sprites.EraseSprite(FIRSTDIGGER + n - game.CurrentPlayer);
            }
        }

        public void DoDiggers(Bags bags, Monsters monsters, Scores scores)
        {
            game.NewFrame();
            if (game.IsGauntletMode)
            {
                DrawLives();
                if (cgtime < game.timer.FrameTime)
                    game.IsTimeOut = true;
                cgtime -= game.timer.FrameTime;
            }
            for (int n = game.CurrentPlayer; n < game.DiggerCount + game.CurrentPlayer; n++)
            {
                if (digdat[n].bob.expsn != 0)
                    drawexplosion(n);
                else
                    UpdateFire(n, monsters, scores);
                if (digvisible)
                {
                    if (digdat[n].dob.alive)
                        if (digdat[n].bagtime != 0)
                        {
                            int tdir = digdat[n].dob.dir;
                            digdat[n].dob.dir = digdat[n].mdir;
                            DrawDigger(n);
                            digdat[n].dob.dir = tdir;
                            game.IncreasePenalty();
                            digdat[n].bagtime--;
                        }
                        else
                            updatedigger(n, bags, monsters, scores);
                    else
                        diggerdie(n, bags, monsters);
                }
                if (digdat[n].emocttime > 0)
                    digdat[n].emocttime--;
            }
            if (bonusmode && IsAlive())
            {
                if (bonustimeleft != 0)
                {
                    bonustimeleft--;
                    if (startbonustimeleft != 0 || bonustimeleft < 20)
                    {
                        startbonustimeleft--;
                        if ((bonustimeleft & 1) != 0)
                        {
                            video.SetIntensity(VideoIntensity.Normal);
                            sound.soundbonus();
                        }
                        else
                        {
                            video.SetIntensity(VideoIntensity.High);
                            sound.soundbonus();
                        }
                        if (startbonustimeleft == 0)
                        {
                            sound.music(0);
                            sound.soundbonusoff();
                            video.SetIntensity(VideoIntensity.High);
                        }
                    }
                }
                else
                {
                    endbonusmode();
                    sound.soundbonusoff();
                    sound.music(1);
                }
            }
            if (bonusmode && !IsAlive())
            {
                endbonusmode();
                sound.soundbonusoff();
                sound.music(1);
            }
        }

        private void UpdateFire(int n, Monsters monsters, Scores scores)
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
                    if (getfirepflag(n - game.CurrentPlayer))
                    {
                        if (digdat[n].dob.alive)
                        {
                            digdat[n].dob.discharge();
                            digdat[n].rechargetime = level.LevelOf10() * 3 + 60;
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
                            sound.soundfire(n);
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
                        pix = video.GetPixel(digdat[n].bob.x, digdat[n].bob.y + 4) |
                            video.GetPixel(digdat[n].bob.x + 4, digdat[n].bob.y + 4);
                        break;
                    case DIR_UP:
                        digdat[n].bob.y -= 7;
                        pix = 0;
                        for (i = 0; i < 7; i++)
                            pix |= video.GetPixel(digdat[n].bob.x + 4, digdat[n].bob.y + i);
                        pix &= 0xc0;
                        break;
                    case DIR_LEFT:
                        digdat[n].bob.x -= 8;
                        pix = video.GetPixel(digdat[n].bob.x, digdat[n].bob.y + 4) |
                            video.GetPixel(digdat[n].bob.x + 4, digdat[n].bob.y + 4);
                        break;
                    case DIR_DOWN:
                        digdat[n].bob.y += 7;
                        pix = 0;
                        for (i = 0; i < 7; i++)
                            pix |= video.GetPixel(digdat[n].bob.x, digdat[n].bob.y + i);
                        pix &= 0x3;
                        break;
                }
                digdat[n].bob.animate();
                for (i = 0; i < TYPES; i++)
                    clfirst[i] = sprites.first[i];
                for (i = 0; i < SPRITES; i++)
                    clcoll[i] = sprites.coll[i];
                game.IncreasePenalty();
                i = clfirst[2];
                while (i != -1)
                {
                    monsters.KillMonster(i - FIRSTMONSTER);
                    scores.ScoreKill(n);
                    digdat[n].bob.explode();
                    i = clcoll[i];
                }
                i = clfirst[4];
                while (i != -1)
                {
                    if (i - FIRSTDIGGER + game.CurrentPlayer != n && !digdat[i - FIRSTDIGGER + game.CurrentPlayer].invin
                        && digdat[i - FIRSTDIGGER + game.CurrentPlayer].dob.alive)
                    {
                        KillDigger(i - FIRSTDIGGER + game.CurrentPlayer, 3, 0);
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
                        if (digdat[i - FIRSTFIREBALL + game.CurrentPlayer].bob.expsn == 0)
                        {
                            digdat[i - FIRSTFIREBALL + game.CurrentPlayer].bob.explode();
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

        public void erasediggers()
        {
            int i;
            for (i = 0; i < game.DiggerCount; i++)
                sprites.EraseSprite(FIRSTDIGGER + i);

            digvisible = false;
        }

        public void drawexplosion(int n)
        {
            if (digdat[n].bob.expsn < 4)
            {
                digdat[n].bob.animate();
                game.IncreasePenalty();
            }
            else
            {
                killfire(n);
            }
        }

        public void killfire(int n)
        {
            if (!digdat[n].notfiring)
            {
                digdat[n].notfiring = true;
                digdat[n].bob.remove();
            }
        }

        private void updatedigger(int n, Bags bags, Monsters monsters, Scores scores)
        {
            int dir, ddir, diggerox, diggeroy, nmon;
            bool push = true, bagf;
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            input.readdirect(n - game.CurrentPlayer);
            dir = input.getdirect(n - game.CurrentPlayer);
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
                video.EatField(diggerox, diggeroy, digdat[n].mdir);
            switch (digdat[n].mdir)
            {
                case DIR_RIGHT:
                    video.DrawRightBlob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.x += 4;
                    break;
                case DIR_UP:
                    video.DrawTopBlob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.y -= 3;
                    break;
                case DIR_LEFT:
                    video.DrawLeftBlob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.x -= 4;
                    break;
                case DIR_DOWN:
                    video.DrawBottomBlob(digdat[n].dob.x, digdat[n].dob.y);
                    digdat[n].dob.y += 3;
                    break;
            }
            if (game.emeralds.HitEmerald((digdat[n].dob.x - 12) / 20, (digdat[n].dob.y - 18) / 18,
                           (digdat[n].dob.x - 12) % 20, (digdat[n].dob.y - 18) % 18,
                           digdat[n].mdir))
            {
                if (digdat[n].emocttime == 0)
                    digdat[n].emn = 0;
                scores.ScoreEmerald(n);
                sound.soundem();
                sound.soundemerald(digdat[n].emn);

                digdat[n].emn++;
                if (digdat[n].emn == 8)
                {
                    digdat[n].emn = 0;
                    scores.ScoreOctave(n);
                }
                digdat[n].emocttime = 9;
            }
            DrawDigger(n);
            for (int i = 0; i < TYPES; i++)
                clfirst[i] = sprites.first[i];
            for (int i = 0; i < SPRITES; i++)
                clcoll[i] = sprites.coll[i];
            game.IncreasePenalty();

            int j = clfirst[1];
            bagf = false;
            while (j != -1)
            {
                if (bags.BagExists(j - Const.FIRSTBAG))
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
                    push = bags.PushBags(digdat[n].mdir, clfirst, clcoll);
                    digdat[n].bagtime++;
                }
                else
                  if (!bags.PushBagsUp(clfirst, clcoll))
                    push = false;
                if (!push)
                { /* Strange, push not completely defined */
                    digdat[n].dob.x = diggerox;
                    digdat[n].dob.y = diggeroy;
                    digdat[n].dob.dir = digdat[n].mdir;
                    DrawDigger(n);
                    game.IncreasePenalty();
                    digdat[n].dob.dir = reversedir(digdat[n].mdir);
                }
            }
            if (clfirst[2] != -1 && bonusmode && digdat[n].dob.alive)
                for (nmon = monsters.KillMonsters(clfirst, clcoll); nmon != 0; nmon--)
                {
                    sound.soundeatm();
                    sceatm(n, scores);
                }
            if (clfirst[0] != -1)
            {
                scores.ScoreBonus(n);
                initbonusmode();
            }
            digdat[n].h = (digdat[n].dob.x - 12) / 20;
            digdat[n].rx = (digdat[n].dob.x - 12) % 20;
            digdat[n].v = (digdat[n].dob.y - 18) / 18;
            digdat[n].ry = (digdat[n].dob.y - 18) % 18;
        }

        public void sceatm(int n, Scores scores)
        {
            scores.ScoreEatMonster(n, digdat[n].msc);
            digdat[n].msc <<= 1;
        }

        public int[] deatharc = { 3, 5, 6, 6, 5, 3, 0 };

        private void diggerdie(int n, Bags bags, Monsters monsters)
        {
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            bool alldead;
            switch (digdat[n].deathstage)
            {
                case 1:
                    if (bags.GetBagY(digdat[n].deathbag) + 6 > digdat[n].dob.y)
                        digdat[n].dob.y = bags.GetBagY(digdat[n].deathbag) + 6;
                    video.DrawDigger(n - game.CurrentPlayer, 15, digdat[n].dob.x, digdat[n].dob.y, false);
                    game.IncreasePenalty();
                    if (bags.GetBagDirection(digdat[n].deathbag) + 1 == 0)
                    {
                        sound.soundddie();
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
                        sound.music(2);
                    video.DrawDigger(n - game.CurrentPlayer, 14 - digdat[n].deathani, digdat[n].dob.x, digdat[n].dob.y,
                               false);
                    for (int i = 0; i < TYPES; i++)
                        clfirst[i] = sprites.first[i];
                    for (int i = 0; i < SPRITES; i++)
                        clcoll[i] = sprites.coll[i];
                    game.IncreasePenalty();
                    if (digdat[n].deathani == 0 && clfirst[2] != -1)
                        monsters.KillMonsters(clfirst, clcoll);
                    if (digdat[n].deathani < 4)
                    {
                        digdat[n].deathani++;
                        digdat[n].deathtime = 2;
                    }
                    else
                    {
                        digdat[n].deathstage = 4;
                        if (sound.musicflag || game.DiggerCount > 1)
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
                        video.DrawDigger(n - game.CurrentPlayer, 15, digdat[n].dob.x,
                                   digdat[n].dob.y - deatharc[digdat[n].deathani], false);
                        if (digdat[n].deathani == 6 && !IsAlive())
                            sound.musicoff();
                        game.IncreasePenalty();
                        digdat[n].deathani++;
                        if (digdat[n].deathani == 1)
                            sound.soundddie();
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
                        for (int i = 0; i < game.DiggerCount; i++)
                            if (!digdat[i].dead)
                            {
                                alldead = false;
                                break;
                            }
                        if (alldead)
                            game.SetDead(true);
                        else if (IsAlive() && digdat[n].lives > 0)
                        {
                            if (!game.IsGauntletMode)
                                digdat[n].lives--;
                            DrawLives();
                            if (digdat[n].lives > 0)
                            {
                                digdat[n].v = 9;
                                digdat[n].mdir = 4;
                                digdat[n].h = (game.DiggerCount == 1) ? 7 : (8 - n * 2);
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
                                sprites.EraseSprite(n + FIRSTDIGGER - game.CurrentPlayer);
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
                                sound.music(0);
                            else
                                sound.music(1);
                        }
                    }
                    break;
            }
        }

        public void createbonus()
        {
            bonusvisible = true;
            video.DrawBonus(292, 18);
        }

        private void initbonusmode()
        {
            int i;
            bonusmode = true;
            erasebonus();
            video.SetIntensity(VideoIntensity.High);
            bonustimeleft = 250 - level.LevelOf10() * 20;
            startbonustimeleft = 20;
            for (i = 0; i < game.DiggerCount; i++)
                digdat[i].msc = 1;
        }

        private void endbonusmode()
        {
            bonusmode = false;
            video.SetIntensity(0);
        }

        public void erasebonus()
        {
            if (bonusvisible)
            {
                bonusvisible = false;
                sprites.EraseSprite(Const.FIRSTBONUS);
            }
            video.SetIntensity(VideoIntensity.Normal);
        }

        public int reversedir(int dir)
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

        public bool checkdiggerunderbag(int h, int v)
        {
            for (int n = game.CurrentPlayer; n < game.DiggerCount + game.CurrentPlayer; n++)
                if (digdat[n].dob.alive)
                    if (digdat[n].mdir == DIR_UP || digdat[n].mdir == DIR_DOWN)
                        if ((digdat[n].dob.x - 12) / 20 == h)
                            if ((digdat[n].dob.y - 18) / 18 == v || (digdat[n].dob.y - 18) / 18 + 1 == v)
                                return true;
            return false;
        }

        public void KillDigger(int n, int stage, int bag)
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

        bool getfirepflag(int n)
        {
            return n == 0 ? input.firepflag : input.fire2pflag;
        }

        public int DiggerX(int n)
        {
            return digdat[n].dob.x;
        }

        public int DiggerY(int n)
        {
            return digdat[n].dob.y;
        }

        public bool IsDiggerAlive(int n)
        {
            return digdat[n].dob.alive;
        }

        public void ResetDiggerTime(int n)
        {
            digdat[n].bagtime = 0;
        }

        public bool IsAlive()
        {
            int i;
            for (i = game.CurrentPlayer; i < game.DiggerCount + game.CurrentPlayer; i++)
                if (digdat[i].dob.alive)
                    return true;

            return false;
        }

        public int GetLives(int pl)
        {
            return digdat[pl].lives;
        }

        public void addlife(int pl)
        {
            digdat[pl].lives++;
            sound.sound1up();
        }

        public void InitializeLives()
        {
            int i;
            for (i = 0; i < game.DiggerCount + game.PlayerCount - 1; i++)
                digdat[i].lives = 3;
        }

        public void DecreaseLife(int player)
        {
            if (!game.IsGauntletMode)
                digdat[player].lives--;
        }

        public void DrawLives()
        {
            int l, n, g;
            if (game.IsGauntletMode)
            {
                g = (int)(cgtime / 1193181);
                string buf = string.Format("{0:D3}:{1:D2}", g / 60, g % 60);
                video.TextOut(buf, 124, 0, 3);
                return;
            }
            n = GetLives(0) - 1;
            video.EraseText(5, 96, 0, 2);
            if (n > 4)
            {
                video.DrawLife(0, 80, 0);
                string buf = string.Format("0x{0:X4}", n);
                video.TextOut(buf, 100, 0, 2);
            }
            else
            {
                for (l = 1; l < 5; l++)
                {
                    video.DrawLife(n > 0 ? 0 : 2, l * 20 + 60, 0);
                    n--;
                }
            }

            if (game.PlayerCount == 2)
            {
                video.EraseText(5, 164, 0, 2);
                n = GetLives(1) - 1;
                if (n > 4)
                {
                    string buf = string.Format("0x{0:X4}", n);
                    video.TextOut(buf, 220 - buf.Length * Const.CHR_W, 0, 2);
                    video.DrawLife(1, 224, 0);
                }
                else
                {
                    for (l = 1; l < 5; l++)
                    {
                        video.DrawLife(n > 0 ? 1 : 2, 244 - l * 20, 0);
                        n--;
                    }
                }
            }

            if (game.DiggerCount == 2)
            {
                video.EraseText(5, 164, 0, 1);
                n = GetLives(1) - 1;
                if (n > 4)
                {
                    string buf = string.Format("0x{0:X4}", n);
                    video.TextOut(buf, 220 - buf.Length * Const.CHR_W, 0, 1);
                    video.DrawLife(3, 224, 0);
                }
                else
                {
                    for (l = 1; l < 5; l++)
                    {
                        video.DrawLife(n > 0 ? 3 : 2, 244 - l * 20, 0);
                        n--;
                    }
                }
            }
        }
    }
}