/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System;

namespace Digger.Source
{
    public class Diggers
    {
        private struct DiggerStruct
        {
            public int x, y, h, v, rx, ry, dir, mdir, bagtime, rechargetime,
                  deathstage, deathBagIndex, deathani, deathtime, emocttime, emn, msc, lives, ivt;
            public bool notfiring, firepressed, dead, invin, isAlive, canFire;
            public Bullet bullet;
        }

        private const int MONSTERS = Const.MONSTERS;
        private const int TYPES = Const.TYPES;
        private const int SPRITES = Const.SPRITES;
        private const int DIGGERS = Const.DIGGERS;
        private const int MSIZE = Const.MSIZE;
        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int FIRSTDIGGER = Const.FIRSTDIGGER;
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int FIRSTFIREBALL = Const.FIRSTFIREBALL;

        private readonly DiggerStruct[] diggerData = new DiggerStruct[DIGGERS];

        public int startbonustimeleft = 0;
        public int bonustimeleft;

        public bool isBonusVisible;
        public bool isBonusMode;
        public bool isDiggerVisible;
        public uint cgtime;

        private readonly Game game;

        public Diggers(Game game)
        {
            this.game = game;
        }

        public void InitializeDiggers()
        {
            for (int dig = game.currentPlayer; dig < game.diggerCount + game.currentPlayer; dig++)
            {
                if (diggerData[dig].lives == 0)
                    continue;

                diggerData[dig].v = 9;
                diggerData[dig].mdir = 4;
                diggerData[dig].h = (game.diggerCount == 1) ? 7 : (8 - dig * 2);
                int x = diggerData[dig].h * 20 + 12;
                int y = diggerData[dig].v * 18 + 18;
                int dir = (dig == 0) ? Dir.Right : Dir.Left;
                diggerData[dig].rx = 0;
                diggerData[dig].ry = 0;
                diggerData[dig].bagtime = 0;
                diggerData[dig].isAlive = true;
                diggerData[dig].canFire = true;
                diggerData[dig].dead = false; /* alive !=> !dead but dead => !alive */
                diggerData[dig].invin = false;
                diggerData[dig].ivt = 0;
                diggerData[dig].deathstage = 1;
                diggerData[dig].x = x;
                diggerData[dig].y = y;
                diggerData[dig].dir = dir;
                diggerData[dig].bullet = new Bullet(game, dig - game.currentPlayer, dir, x, y);
                diggerData[dig].notfiring = true;
                diggerData[dig].emocttime = 0;
                diggerData[dig].bullet.expsn = 0;
                diggerData[dig].firepressed = false;
                diggerData[dig].rechargetime = 0;
                diggerData[dig].emn = 0;
                diggerData[dig].msc = 1;
                game.sprite.MoveDrawSprite(Const.FIRSTDIGGER + dig, x, y);
            }

            isDiggerVisible = true;
            isBonusVisible = isBonusMode = false;
        }

        public void DrawDigger(int n)
        {
            game.drawing.DrawDigger(n, diggerData[n].dir, diggerData[n].x, diggerData[n].y, diggerData[n].canFire);
            if (diggerData[n].invin)
            {
                diggerData[n].ivt--;
                if (diggerData[n].ivt == 0)
                    diggerData[n].invin = false;
                else
                  if (diggerData[n].ivt % 10 < 5)
                    game.sprite.EraseSprite(FIRSTDIGGER + n - game.currentPlayer);
            }
        }

        public void DoDiggers()
        {
            if (game.isGauntletMode)
            {
                DrawLives();
                if (cgtime < game.timer.FrameTicks)
                    game.isTimeOut = true;
                cgtime -= game.timer.FrameTicks;
            }
            for (int n = game.currentPlayer; n < game.diggerCount + game.currentPlayer; n++)
            {
                if (diggerData[n].bullet.expsn != 0)
                    DrawExplosion(n);
                else
                    UpdateFire(n);
                if (isDiggerVisible)
                {
                    if (diggerData[n].isAlive)
                        if (diggerData[n].bagtime != 0)
                        {
                            int tdir = diggerData[n].dir;
                            diggerData[n].dir = diggerData[n].mdir;
                            DrawDigger(n);
                            diggerData[n].dir = tdir;
                            game.IncrementPenalty();
                            diggerData[n].bagtime--;
                        }
                        else
                            UpdateDigger(n);
                    else
                        DiggerDie(n);
                }
                if (diggerData[n].emocttime > 0)
                    diggerData[n].emocttime--;
            }
            if (isBonusMode && IsAnyAlive())
            {
                if (bonustimeleft != 0)
                {
                    bonustimeleft--;
                    if (startbonustimeleft != 0 || bonustimeleft < 20)
                    {
                        startbonustimeleft--;
                        if ((bonustimeleft & 1) != 0)
                        {
                            game.video.SetIntensity(0);
                            game.sound.SoundBonus();
                        }
                        else
                        {
                            game.video.SetIntensity(1);
                            game.sound.SoundBonus();
                        }
                        if (startbonustimeleft == 0)
                        {
                            game.sound.Music(0);
                            game.sound.SoundBonusOff();
                            game.video.SetIntensity(1);
                        }
                    }
                }
                else
                {
                    EndBonusMode();
                    game.sound.SoundBonusOff();
                    game.sound.Music(1);
                }
            }
            if (isBonusMode && !IsAnyAlive())
            {
                EndBonusMode();
                game.sound.SoundBonusOff();
                game.sound.Music(1);
            }
        }

        private void UpdateFire(int n)
        {
            int pix, fx = 0, fy = 0;
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            int i;
            bool clflag;
            if (diggerData[n].notfiring)
            {
                if (diggerData[n].rechargetime != 0)
                {
                    diggerData[n].rechargetime--;
                    if (diggerData[n].rechargetime == 0)
                    {
                        diggerData[n].canFire = true;
                    }
                }
                else
                {
                    if (GetFirepFlag(n - game.currentPlayer))
                    {
                        if (diggerData[n].isAlive)
                        {
                            diggerData[n].canFire = false;
                            diggerData[n].rechargetime = Level.LevelOf10(game.Level) * 3 + 60;
                            diggerData[n].notfiring = false;
                            switch (diggerData[n].dir)
                            {
                                case Dir.Right:
                                    fx = diggerData[n].x + 8;
                                    fy = diggerData[n].y + 4;
                                    break;
                                case Dir.Up:
                                    fx = diggerData[n].x + 4;
                                    fy = diggerData[n].y;
                                    break;
                                case Dir.Left:
                                    fx = diggerData[n].x;
                                    fy = diggerData[n].y + 4;
                                    break;
                                case Dir.Down:
                                    fx = diggerData[n].x + 4;
                                    fy = diggerData[n].y + 8;
                                    break;
                                default:
                                    throw new NotSupportedException($"Direction '{diggerData[n].dir}' is not supported.");
                            }
                            diggerData[n].bullet.Update(diggerData[n].dir, fx, fy);
                            diggerData[n].bullet.Put();
                        }
                    }
                }
            }
            else
            {
                pix = 0;
                switch (diggerData[n].bullet.dir)
                {
                    case Dir.Right:
                        diggerData[n].bullet.x += 8;
                        pix = game.video.GetPixel(diggerData[n].bullet.x, diggerData[n].bullet.y + 4) |
                            game.video.GetPixel(diggerData[n].bullet.x + 4, diggerData[n].bullet.y + 4);
                        break;
                    case Dir.Up:
                        diggerData[n].bullet.y -= 7;
                        pix = 0;
                        for (i = 0; i < 7; i++)
                            pix |= game.video.GetPixel(diggerData[n].bullet.x + 4, diggerData[n].bullet.y + i);
                        pix &= 0xc0;
                        break;
                    case Dir.Left:
                        diggerData[n].bullet.x -= 8;
                        pix = game.video.GetPixel(diggerData[n].bullet.x, diggerData[n].bullet.y + 4) |
                            game.video.GetPixel(diggerData[n].bullet.x + 4, diggerData[n].bullet.y + 4);
                        break;
                    case Dir.Down:
                        diggerData[n].bullet.y += 7;
                        pix = 0;
                        for (i = 0; i < 7; i++)
                            pix |= game.video.GetPixel(diggerData[n].bullet.x, diggerData[n].bullet.y + i);
                        pix &= 0x3;
                        break;
                }
                diggerData[n].bullet.Animate();
                for (i = 0; i < TYPES; i++)
                    clfirst[i] = game.sprite.first[i];
                for (i = 0; i < SPRITES; i++)
                    clcoll[i] = game.sprite.coll[i];
                game.IncrementPenalty();
                i = clfirst[2];
                while (i != -1)
                {
                    game.monsters.KillMonster(i - FIRSTMONSTER);
                    game.scores.ScoreKill(n);
                    diggerData[n].bullet.Explode();
                    i = clcoll[i];
                }
                i = clfirst[4];
                while (i != -1)
                {
                    if (i - FIRSTDIGGER + game.currentPlayer != n && !diggerData[i - FIRSTDIGGER + game.currentPlayer].invin
                        && diggerData[i - FIRSTDIGGER + game.currentPlayer].isAlive)
                    {
                        KillDigger(i - FIRSTDIGGER + game.currentPlayer, 3, 0);
                        diggerData[n].bullet.Explode();
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
                    diggerData[n].bullet.Explode();
                    i = clfirst[3];
                    while (i != -1)
                    {
                        if (diggerData[i - FIRSTFIREBALL + game.currentPlayer].bullet.expsn == 0)
                        {
                            diggerData[i - FIRSTFIREBALL + game.currentPlayer].bullet.Explode();
                        }
                        i = clcoll[i];
                    }
                }
                switch (diggerData[n].bullet.dir)
                {
                    case Dir.Right:
                        if (diggerData[n].bullet.x > 296)
                        {
                            diggerData[n].bullet.Explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                diggerData[n].bullet.x -= 8;
                                diggerData[n].bullet.Animate();
                                diggerData[n].bullet.Explode();
                            }
                        }
                        break;
                    case Dir.Up:
                        if (diggerData[n].bullet.y < 15)
                        {
                            diggerData[n].bullet.Explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                diggerData[n].bullet.y += 7;
                                diggerData[n].bullet.Animate();
                                diggerData[n].bullet.Explode();
                            }
                        }
                        break;
                    case Dir.Left:
                        if (diggerData[n].bullet.x < 16)
                        {
                            diggerData[n].bullet.Explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                diggerData[n].bullet.x += 8;
                                diggerData[n].bullet.Animate();
                                diggerData[n].bullet.Explode();
                            }
                        }
                        break;
                    case Dir.Down:
                        if (diggerData[n].bullet.y > 183)
                        {
                            diggerData[n].bullet.Explode();
                        }
                        else
                        {
                            if (pix != 0 && !clflag)
                            {
                                diggerData[n].bullet.y -= 7;
                                diggerData[n].bullet.Animate();
                                diggerData[n].bullet.Explode();
                            }
                        }
                        break;
                }
            }
        }

        public void EraseDiggers()
        {
            int i;
            for (i = 0; i < game.diggerCount; i++)
                game.sprite.EraseSprite(FIRSTDIGGER + i);

            isDiggerVisible = false;
        }

        public void DrawExplosion(int n)
        {
            if (diggerData[n].bullet.expsn < 4)
            {
                diggerData[n].bullet.Animate();
                game.IncrementPenalty();
            }
            else
            {
                KillFire(n);
            }
        }

        public void KillFire(int n)
        {
            if (!diggerData[n].notfiring)
            {
                diggerData[n].notfiring = true;
                diggerData[n].bullet.Remove();
            }
        }

        private void UpdateDigger(int n)
        {
            int dir, ddir, diggerox, diggeroy, nmon;
            bool push = true, bagf;
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            game.input.ReadDirection(n - game.currentPlayer);
            dir = game.input.GetDirection(n - game.currentPlayer);
            if (dir == Dir.Right || dir == Dir.Up || dir == Dir.Left || dir == Dir.Down)
                ddir = dir;
            else
                ddir = Dir.None;
            if (diggerData[n].rx == 0 && (ddir == Dir.Up || ddir == Dir.Down))
                diggerData[n].dir = diggerData[n].mdir = ddir;
            if (diggerData[n].ry == 0 && (ddir == Dir.Right || ddir == Dir.Left))
                diggerData[n].dir = diggerData[n].mdir = ddir;
            if (dir == Dir.None)
                diggerData[n].mdir = Dir.None;
            else
                diggerData[n].mdir = diggerData[n].dir;
            if ((diggerData[n].x == 292 && diggerData[n].mdir == Dir.Right) ||
                (diggerData[n].x == 12 && diggerData[n].mdir == Dir.Left) ||
                (diggerData[n].y == 180 && diggerData[n].mdir == Dir.Down) ||
                (diggerData[n].y == 18 && diggerData[n].mdir == Dir.Up))
                diggerData[n].mdir = Dir.None;
            diggerox = diggerData[n].x;
            diggeroy = diggerData[n].y;
            if (diggerData[n].mdir != Dir.None)
                game.drawing.EatField(diggerox, diggeroy, diggerData[n].mdir);
            switch (diggerData[n].mdir)
            {
                case Dir.Right:
                    game.drawing.DrawRightBlob(diggerData[n].x, diggerData[n].y);
                    diggerData[n].x += 4;
                    break;
                case Dir.Up:
                    game.drawing.DrawTopBlob(diggerData[n].x, diggerData[n].y);
                    diggerData[n].y -= 3;
                    break;
                case Dir.Left:
                    game.drawing.DrawLeftBlob(diggerData[n].x, diggerData[n].y);
                    diggerData[n].x -= 4;
                    break;
                case Dir.Down:
                    game.drawing.DrawBottomBlob(diggerData[n].x, diggerData[n].y);
                    diggerData[n].y += 3;
                    break;
            }
            if (game.emeralds.IsEmeraldHit((diggerData[n].x - 12) / 20, (diggerData[n].y - 18) / 18,
                           (diggerData[n].x - 12) % 20, (diggerData[n].y - 18) % 18,
                           diggerData[n].mdir))
            {
                if (diggerData[n].emocttime == 0)
                    diggerData[n].emn = 0;

                game.scores.ScoreEmerald(n);
                game.sound.SoundEmerald(diggerData[n].emn);

                diggerData[n].emn++;
                if (diggerData[n].emn == 8)
                {
                    diggerData[n].emn = 0;
                    game.scores.ScoreOctave(n);
                }

                diggerData[n].emocttime = 9;
            }
            DrawDigger(n);
            for (int i = 0; i < TYPES; i++)
                clfirst[i] = game.sprite.first[i];
            for (int i = 0; i < SPRITES; i++)
                clcoll[i] = game.sprite.coll[i];
            game.IncrementPenalty();

            int j = clfirst[1];
            bagf = false;
            while (j != -1)
            {
                if (game.bags.BagExists(j - Const.FIRSTBAG))
                {
                    bagf = true;
                    break;
                }
                j = clcoll[j];
            }

            if (bagf)
            {
                if (diggerData[n].mdir == Dir.Right || diggerData[n].mdir == Dir.Left)
                {
                    push = game.bags.PushBags(diggerData[n].mdir, clfirst, clcoll);
                    diggerData[n].bagtime++;
                }
                else
                  if (!game.bags.PushBagsUp(clfirst, clcoll))
                    push = false;
                if (!push)
                { /* Strange, push not completely defined */
                    diggerData[n].x = diggerox;
                    diggerData[n].y = diggeroy;
                    diggerData[n].dir = diggerData[n].mdir;
                    DrawDigger(n);
                    game.IncrementPenalty();
                    diggerData[n].dir = Dir.Reverse(diggerData[n].mdir);
                }
            }
            if (clfirst[2] != -1 && isBonusMode && diggerData[n].isAlive)
                for (nmon = game.monsters.KillMonsters(clfirst, clcoll); nmon != 0; nmon--)
                {
                    game.sound.SoundEatMonster();
                    ScoreEatMonster(n);
                }
            if (clfirst[0] != -1)
            {
                game.scores.ScoreBonus(n);
                InitializeBonusMode();
            }
            diggerData[n].h = (diggerData[n].x - 12) / 20;
            diggerData[n].rx = (diggerData[n].x - 12) % 20;
            diggerData[n].v = (diggerData[n].y - 18) / 18;
            diggerData[n].ry = (diggerData[n].y - 18) % 18;
        }

        public void ScoreEatMonster(int n)
        {
            game.scores.ScoreEatMonster(n, diggerData[n].msc);
            diggerData[n].msc <<= 1;
        }

        public int[] deatharc = { 3, 5, 6, 6, 5, 3, 0 };

        private void DiggerDie(int n)
        {
            int[] clfirst = new int[TYPES];
            int[] clcoll = new int[SPRITES];
            bool alldead;
            switch (diggerData[n].deathstage)
            {
                case 1:
                    if (game.bags.GetBagY(diggerData[n].deathBagIndex) + 6 > diggerData[n].y)
                        diggerData[n].y = game.bags.GetBagY(diggerData[n].deathBagIndex) + 6;

                    game.drawing.DrawDigger(n - game.currentPlayer, 15, diggerData[n].x, diggerData[n].y, false);
                    game.IncrementPenalty();
                    if (game.bags.GetBagDirection(diggerData[n].deathBagIndex) + 1 == 0)
                    {
                        game.sound.SoundDiggerDie();
                        diggerData[n].deathtime = 5;
                        diggerData[n].deathstage = 2;
                        diggerData[n].deathani = 0;
                        diggerData[n].y -= 6;
                    }
                    break;
                case 2:
                    if (diggerData[n].deathtime != 0)
                    {
                        diggerData[n].deathtime--;
                        break;
                    }
                    if (diggerData[n].deathani == 0)
                        game.sound.Music(2);

                    game.drawing.DrawDigger(n - game.currentPlayer, 14 - diggerData[n].deathani, diggerData[n].x, diggerData[n].y, false);

                    for (int i = 0; i < TYPES; i++)
                        clfirst[i] = game.sprite.first[i];

                    for (int i = 0; i < SPRITES; i++)
                        clcoll[i] = game.sprite.coll[i];

                    game.IncrementPenalty();
                    if (diggerData[n].deathani == 0 && clfirst[2] != -1)
                        game.monsters.KillMonsters(clfirst, clcoll);

                    if (diggerData[n].deathani < 4)
                    {
                        diggerData[n].deathani++;
                        diggerData[n].deathtime = 2;
                    }
                    else
                    {
                        diggerData[n].deathstage = 4;
                        if (game.sound.isMusicEnabled || game.diggerCount > 1)
                            diggerData[n].deathtime = 60;
                        else
                            diggerData[n].deathtime = 10;
                    }
                    break;
                case 3:
                    diggerData[n].deathstage = 5;
                    diggerData[n].deathani = 0;
                    diggerData[n].deathtime = 0;
                    break;
                case 5:
                    if (diggerData[n].deathani >= 0 && diggerData[n].deathani <= 6)
                    {
                        game.drawing.DrawDigger(n - game.currentPlayer, 15, diggerData[n].x, diggerData[n].y - deatharc[diggerData[n].deathani], false);
                        if (diggerData[n].deathani == 6 && !IsAnyAlive())
                            game.sound.MusicOff();
                        game.IncrementPenalty();
                        diggerData[n].deathani++;
                        if (diggerData[n].deathani == 1)
                            game.sound.SoundDiggerDie();

                        if (diggerData[n].deathani == 7)
                        {
                            diggerData[n].deathtime = 5;
                            diggerData[n].deathani = 0;
                            diggerData[n].deathstage = 2;
                        }
                    }
                    break;
                case 4:
                    if (diggerData[n].deathtime != 0)
                        diggerData[n].deathtime--;
                    else
                    {
                        diggerData[n].dead = true;
                        alldead = true;
                        for (int i = 0; i < game.diggerCount; i++)
                            if (!diggerData[i].dead)
                            {
                                alldead = false;
                                break;
                            }
                        if (alldead)
                            game.SetDead(true);
                        else if (IsAnyAlive() && diggerData[n].lives > 0)
                        {
                            if (!game.isGauntletMode)
                                diggerData[n].lives--;
                            DrawLives();
                            if (diggerData[n].lives > 0)
                            {
                                diggerData[n].v = 9;
                                diggerData[n].mdir = 4;
                                diggerData[n].h = (game.diggerCount == 1) ? 7 : (8 - n * 2);
                                diggerData[n].x = diggerData[n].h * 20 + 12;
                                diggerData[n].dir = (n == 0) ? Dir.Right : Dir.Left;
                                diggerData[n].rx = 0;
                                diggerData[n].ry = 0;
                                diggerData[n].bagtime = 0;
                                diggerData[n].canFire = true;
                                diggerData[n].isAlive = true;
                                diggerData[n].dead = false;
                                diggerData[n].invin = true;
                                diggerData[n].ivt = 50;
                                diggerData[n].deathstage = 1;
                                diggerData[n].y = diggerData[n].v * 18 + 18;
                                game.sprite.EraseSprite(n + FIRSTDIGGER - game.currentPlayer);
                                diggerData[n].notfiring = true;
                                diggerData[n].emocttime = 0;
                                diggerData[n].firepressed = false;
                                diggerData[n].bullet.expsn = 0;
                                diggerData[n].rechargetime = 0;
                                diggerData[n].emn = 0;
                                diggerData[n].msc = 1;
                                game.sprite.MoveDrawSprite(Const.FIRSTDIGGER + n, diggerData[n].x, diggerData[n].x);
                            }
                            game.input.ClearFire(n);
                            if (isBonusMode)
                                game.sound.Music(0);
                            else
                                game.sound.Music(1);
                        }
                    }
                    break;
            }
        }

        public void CreateBonus()
        {
            isBonusVisible = true;
            game.drawing.DrawBonus(292, 18);
        }

        private void InitializeBonusMode()
        {
            isBonusMode = true;
            EraseBonus();
            game.video.SetIntensity(1);
            bonustimeleft = 250 - Level.LevelOf10(game.Level) * 20;
            startbonustimeleft = 20;
            for (int i = 0; i < game.diggerCount; i++)
                diggerData[i].msc = 1;
        }

        private void EndBonusMode()
        {
            isBonusMode = false;
            game.video.SetIntensity(0);
        }

        public void EraseBonus()
        {
            if (isBonusVisible)
            {
                isBonusVisible = false;
                game.sprite.EraseSprite(Const.FIRSTBONUS);
            }
            game.video.SetIntensity(0);
        }

        public bool CheckIsDiggerUnderBag(int h, int v)
        {
            for (int n = game.currentPlayer; n < game.diggerCount + game.currentPlayer; n++)
                if (diggerData[n].isAlive)
                    if (diggerData[n].mdir == Dir.Up || diggerData[n].mdir == Dir.Down)
                        if ((diggerData[n].x - 12) / 20 == h)
                            if ((diggerData[n].y - 18) / 18 == v || (diggerData[n].y - 18) / 18 + 1 == v)
                                return true;
            return false;
        }

        public void KillDigger(int n, int stage, int bag)
        {
            if (diggerData[n].invin)
                return;
            if (diggerData[n].deathstage < 2 || diggerData[n].deathstage > 4)
            {
                diggerData[n].isAlive = false;
                diggerData[n].deathstage = stage;
                diggerData[n].deathBagIndex = bag;
            }
        }

        bool GetFirepFlag(int n)
        {
            return n == 0 ? game.input.firepflag : game.input.fire2pflag;
        }

        public int DiggerX(int n)
        {
            return diggerData[n].x;
        }

        public int DiggerY(int n)
        {
            return diggerData[n].y;
        }

        public bool IsDiggerAlive(int n)
        {
            return diggerData[n].isAlive;
        }

        public void ResetDiggerTime(int n)
        {
            diggerData[n].bagtime = 0;
        }

        public bool IsAnyAlive()
        {
            for (int i = game.currentPlayer; i < game.diggerCount + game.currentPlayer; i++)
                if (diggerData[i].isAlive)
                    return true;

            return false;
        }

        public int GetLives(int pl)
        {
            return diggerData[pl].lives;
        }

        public void AddLife(int pl)
        {
            diggerData[pl].lives++;
            game.sound.Sound1Up();
        }

        public void InitializeLives()
        {
            int i;
            for (i = 0; i < game.diggerCount + game.playerCount - 1; i++)
                diggerData[i].lives = 3;
        }

        public void DecreaseLife(int player)
        {
            if (!game.isGauntletMode && !game.hasUnlimitedLives)
                diggerData[player].lives--;
        }

        public void DrawLives()
        {
            int l, n, g;
            if (game.isGauntletMode)
            {
                g = (int)(cgtime / 1193181);
                string buf = string.Format("{0:D3}:{1:D2}", g / 60, g % 60);
                game.drawing.TextOut(buf, 124, 0, 3);
                return;
            }
            n = GetLives(0) - 1;
            game.drawing.EraseText(5, 96, 0, 2);
            if (n > 4)
            {
                game.drawing.DrawLife(0, 80, 0);
                string buf = string.Format("0x{0:X4}", n);
                game.drawing.TextOut(buf, 100, 0, 2);
            }
            else
            {
                for (l = 1; l < 5; l++)
                {
                    game.drawing.DrawLife(n > 0 ? 0 : 2, l * 20 + 60, 0);
                    n--;
                }
            }

            if (game.playerCount == 2)
            {
                game.drawing.EraseText(5, 164, 0, 2);
                n = GetLives(1) - 1;
                if (n > 4)
                {
                    string buf = string.Format("0x{0:X4}", n);
                    game.drawing.TextOut(buf, 220 - buf.Length * Const.CHR_W, 0, 2);
                    game.drawing.DrawLife(1, 224, 0);
                }
                else
                {
                    for (l = 1; l < 5; l++)
                    {
                        game.drawing.DrawLife(n > 0 ? 1 : 2, 244 - l * 20, 0);
                        n--;
                    }
                }
            }

            if (game.diggerCount == 2)
            {
                game.drawing.EraseText(5, 164, 0, 1);
                n = GetLives(1) - 1;
                if (n > 4)
                {
                    string buf = string.Format("0x{0:X4}", n);
                    game.drawing.TextOut(buf, 220 - buf.Length * Const.CHR_W, 0, 1);
                    game.drawing.DrawLife(3, 224, 0);
                }
                else
                {
                    for (l = 1; l < 5; l++)
                    {
                        game.drawing.DrawLife(n > 0 ? 3 : 2, 244 - l * 20, 0);
                        n--;
                    }
                }
            }
        }
    }
}