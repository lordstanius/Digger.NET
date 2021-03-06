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

namespace Digger.Source
{
    public class Bullet
    {
        public readonly int id;
        public int expsn;
        public int dir;
        public int x;
        public int y;

        private readonly Game game;
        
        public Bullet(Game game, int id, int dir, int x, int y)
        {
            this.dir = dir;
            this.x = x;
            this.y = y;
            this.id = id;
            this.expsn = 0;
            this.game = game;
        }

        private int SpriteID => Const.FIRSTFIREBALL + id;

        public void Put()
        {
            game.sprite.MoveDrawSprite(SpriteID, x, y);
            game.sound.SoundFire(id);
        }

        public void Animate()
        {
            System.Diagnostics.Debug.Assert(expsn < 4);
            game.drawing.DrawFire(id, x, y, expsn);
            if (expsn > 0)
            {
                if (expsn == 1)
                    game.sound.SoundExplode(id);

                ++expsn;
            }
        }

        public void Remove()
        {
            game.sprite.EraseSprite(SpriteID);
            if (expsn > 1)
                game.sound.SoundFireOff(id);

            expsn = 0;
        }

        public void Explode()
        {
            expsn = 1;
        }

        public void Update(int dir, int fx, int fy)
        {
            this.dir = dir;
            this.x = fx;
            this.y = fy;
        }
    }
}