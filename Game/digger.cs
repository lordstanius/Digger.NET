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

using System;

namespace Digger.Net
{
    public class Digger
    {
        public readonly int id;
        public bool isAlive;
        public bool isZombie;
        public bool canFire;
        public int dir;
        public int x;
        public int y;

        private readonly Game game;

        public Digger(Game game, int id, int dir, int x, int y)
        {
            this.dir = dir;
            this.x = x;
            this.y = y;
            this.id = id;
            this.isAlive = true;
            this.isZombie = false;
            this.canFire = true;
            this.game = game;
        }

        public void Put()
        {
            game.sprites.MoveDrawSprite(Const.FIRSTDIGGER + id, x, y);
        }

        public void Animate()
        {
            game.video.DrawDigger(id, dir, x, y, canFire);
        }

        public void Discharge()
        {

            System.Diagnostics.Debug.Assert(canFire);
            canFire = false;
        }

        public void Recharge()
        {
            System.Diagnostics.Debug.Assert(!canFire);
            canFire = true;
        }

        public Action damage;
        public Action kill;
    };
}