/*
 * Copyright (c) 1983 Windmill Software
 * Copyright (c) 1989-2002 Andrew Jenner <aj@org>
 * Copyright (c) 2002-2014 Maxim Sobolev <sobomax@FreeBSD.org>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

namespace Digger.Net
{
    public struct Position
    {
        public int dir;
        public short x;
        public short y;
    }


    public class Monster
    {
        private const int FIRSTMONSTER = Const.FIRSTMONSTER;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;

        private readonly int id;
        private Position pos;
        private bool isZombie;
        private int monsterSprite;
        private int monsterSpriteDrawn;
        private readonly Game game;

        public Monster(Game game, int id, int dir, short x, short y)
        {
            this.id = id;
            pos.dir = dir;
            pos.x = x;
            pos.y = y;
            IsAlive = true;
            isZombie = false;
            IsNobbin = true;
            monsterSprite = 0;
            monsterSpriteDrawn = 1;
            this.game = game;
        }

        public bool IsNobbin { get; protected set; }
        public bool IsAlive { get; protected set; }
        public Position Position
        {
            get => pos;
            set => pos = value;
        }

        private int SpriteID => FIRSTMONSTER + id;

        private void DrawMonster()
        {
            monsterSprite += monsterSpriteDrawn;
            if (monsterSprite == 2 || monsterSprite == 0)
                monsterSpriteDrawn = -(monsterSpriteDrawn);

            if (monsterSprite > 2)
                monsterSprite = 2;

            if (monsterSprite < 0)
                monsterSprite = 0;

            UpdateSprite();
            game.sprites.DrawSprite(SpriteID, pos.x, pos.y);
        }

        public void DrawMonsterDie()
        {
            UpdateSprite();
            game.sprites.DrawSprite(SpriteID, pos.x, pos.y);
        }

        private void UpdateSprite()
        {
            if (IsAlive)
            {
                if (IsNobbin)
                {
                    game.sprites.InitializeSprite(SpriteID, monsterSprite + 69, 4, 15, 0, 0);
                }
                else
                {
                    switch (pos.dir)
                    {
                        case DIR_RIGHT:
                            game.sprites.InitializeSprite(SpriteID, monsterSprite + 73, 4, 15, 0, 0);
                            break;
                        case DIR_LEFT:
                            game.sprites.InitializeSprite(SpriteID, monsterSprite + 77, 4, 15, 0, 0);
                            break;
                    }
                }
            }
            else if (isZombie)
            {
                if (IsNobbin)
                {
                    game.sprites.InitializeSprite(SpriteID, 72, 4, 15, 0, 0);
                }
                else
                {
                    switch (pos.dir)
                    {
                        case DIR_RIGHT:
                            game.sprites.InitializeSprite(SpriteID, 76, 4, 15, 0, 0);
                            break;
                        case DIR_LEFT:
                            game.sprites.InitializeSprite(SpriteID, 80, 4, 14, 0, 0);
                            break;
                    }
                }
            }
        }

        public void Put()
        {
            UpdateSprite();
            game.sprites.MoveDrawSprite(SpriteID, pos.x, pos.y);
        }

        public void Mutate()
        {
            IsNobbin = !IsNobbin;
            UpdateSprite();
            game.sprites.DrawSprite(SpriteID, pos.x, pos.y);
        }

        public int Damage()
        {
            if (!IsAlive)
            {
                /* We can only damage live thing or try to damage zombie */
                System.Diagnostics.Debug.Assert(isZombie);
            }

            isZombie = true;
            IsAlive = false;
            UpdateSprite();
            game.sprites.DrawSprite(SpriteID, pos.x, pos.y);
            return (0);
        }

        public void Kill()
        {
            if (!IsAlive)
            {
                /* No, you can't kill me twice */
                System.Diagnostics.Debug.Assert(isZombie);
            }

            IsAlive = false;
            isZombie = false;
            game.sprites.EraseSprite(SpriteID);
        }

        public void Animate()
        {
            if (IsAlive)
                DrawMonster();
            else if (isZombie)
                DrawMonsterDie();
        }
    }
}