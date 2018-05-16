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

using System;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public class digger_obj
        {
            public int d_id;
            public bool alive;
            public bool zombie;
            public bool can_fire;
            public int dir;
            public int x;
            public int y;

            public digger_obj(int d_id, int dir, int x, int y)
            {
                this.dir = dir;
                this.x = x;
                this.y = y;
                this.d_id = d_id;
                this.alive = true;
                this.zombie = false;
                this.can_fire = true;
            }

            public void put()
            {
                movedrawspr(FIRSTDIGGER + d_id, x, y);
            }

            public void animate()
            {
                drawdigger(d_id, dir, x, y, can_fire);
            }

            public void discharge()
            {

                System.Diagnostics.Debug.Assert(can_fire);
                can_fire = false;
            }

            public void recharge()
            {
                System.Diagnostics.Debug.Assert(!can_fire);
                can_fire = true;
            }

            public Action damage;
            public Action kill;
        };
    }
}