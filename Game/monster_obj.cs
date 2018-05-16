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
using System.Collections.Generic;
using SDL2;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public const bool MON_NOBBIN = true;
        public const bool MON_HOBBIN = false;

        public struct obj_position
        {
            public int dir;
            public int x;
            public int y;

            public string DIR2STR()
            {
                switch (dir)
                {
                    case DIR_NONE:
                        return "NONE";
                    case DIR_RIGHT:
                        return "RIGHT";
                    case DIR_UP:
                        return "UP";
                    case DIR_LEFT:
                        return "LEFT";
                    case DIR_DOWN:
                        return "DOWN";
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public class monster_obj
        {
            private int m_id;
            private bool nobf;
            private bool alive;
            private bool zombie;
            private obj_position pos;
            private int monspr;
            private int monspd;

            private void _drawmon()
            {
                int sprid = FIRSTMONSTER + m_id;

                monspr += monspd;
                if (monspr == 2 || monspr == 0)
                    monspd = -(monspd);
                if (monspr > 2)
                    monspr = 2;
                if (monspr < 0)
                    monspr = 0;

                updspr();
                drawspr(sprid, pos.x, pos.y);
            }

            public void _drawmondie()
            {
                int sprid = FIRSTMONSTER + m_id;

                updspr();
                drawspr(sprid, pos.x, pos.y);
            }

            public void updspr()
            {
                int sprid;

                sprid = FIRSTMONSTER + m_id;

                if (alive)
                {
                    if (nobf)
                    {
                        initspr(sprid, monspr + 69, 4, 15, 0, 0);
                    }
                    else
                    {
                        switch (pos.dir)
                        {
                            case DIR_RIGHT:
                                initspr(sprid, monspr + 73, 4, 15, 0, 0);
                                break;
                            case DIR_LEFT:
                                initspr(sprid, monspr + 77, 4, 15, 0, 0);
                                break;
                        }
                    }
                }
                else if (zombie)
                {
                    if (nobf)
                    {
                        initspr(sprid, 72, 4, 15, 0, 0);
                    }
                    else
                    {
                        switch (pos.dir)
                        {
                            case DIR_RIGHT:
                                initspr(sprid, 76, 4, 15, 0, 0);
                                break;
                            case DIR_LEFT:
                                initspr(sprid, 80, 4, 14, 0, 0);
                                break;
                        }
                    }
                }
            }

            public monster_obj(int m_id, bool nobf, short dir, short x, short y)
            {
                this.nobf = nobf;
                pos.dir = dir;
                pos.x = x;
                pos.y = y;
                this.m_id = m_id;
                alive = true;
                zombie = false;
                monspr = 0;
                monspd = 1;
            }

            public int put()
            {
                updspr();
                movedrawspr(FIRSTMONSTER + m_id, pos.x, pos.y);
                return (0);
            }

            public int mutate()
            {
                nobf = !nobf;
                updspr();
                drawspr(FIRSTMONSTER + m_id, pos.x, pos.y);
                return (0);
            }

            public int damage()
            {
                if (!alive)
                {
                    /* We can only damage live thing or try to damage zombie */
                    System.Diagnostics.Debug.Assert(zombie);
                }
                zombie = true;
                alive = false;
                updspr();
                drawspr(FIRSTMONSTER + m_id, pos.x, pos.y);
                return (0);
            }

            public int kill()
            {
                if (!alive)
                {
                    /* No, you can't kill me twice */
                    System.Diagnostics.Debug.Assert(zombie);
                }
                alive = false;
                zombie = false;
                erasespr(FIRSTMONSTER + m_id);
                return (0);
            }

            public int animate()
            {
                if (alive)
                    _drawmon();
                else if (zombie)
                    _drawmondie();

                return (0);
            }

            public obj_position getpos()
            {
                return pos;
            }

            public int setpos(obj_position pos)
            {
                this.pos = pos;
                return (0);
            }

            public bool isalive()
            {
                return alive;
            }

            public bool isnobbin()
            {
                return (nobf);
            }
        }
    }
}