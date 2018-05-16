/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;
using System.IO;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public const string DEFAULTSN = "DRF";
        public const int MAX_REC_BUFFER = 262144;

        static readonly int A_minus_a = 'A' - 'a';
        /* I reckon this is enough for about 36 hours of continuous play. */

        public static char[] recb, plb, plp;

        public static bool playing, savedrf, gotname, gotgame, drfvalid, kludge;

        public static string rname;

        public static int reccc, recrl, rlleft;
        public static uint recp;
        public static char recd, rld;

# if INTDRF
        private static FileStream info;
#endif

        static string smart_fgets(FileStream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadLine();
        }

        public static void openplay(string name)
        {
            FileStream playf;
            try
            {
                playf = File.OpenRead(name);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                escape = true;
                return;
            }

            uint origgtime = gtime;
            bool origg = gauntlet;
            int origstartlev = startlev, orignplayers = nplayers, origdiggers = diggers;
# if INTDRF
            info = File.OpenWrite("DRFINFO.TXT");
#endif
            gauntlet = false;
            startlev = 1;
            nplayers = 1;
            diggers = 1;
            /* The file is in two distinct parts. In the first, line breaks are used as
               separators. In the second, they are ignored. This is the first. */

            /* Get id string */
            string buf;
            if ((buf = smart_fgets(playf)) == null)
            {
                goto out_0;
            }
            if (!buf.StartsWith("DRF"))
            {
                goto out_0;
            }
            /* Get version for kludge switches */
            if ((buf = smart_fgets(playf)) == null)
            {
                goto out_0;
            }
            if (int.Parse(buf.Substring(7)) <= 19981125)
                kludge = true;

            /* Get mode */
            if ((buf = smart_fgets(playf)) == null)
            {
                goto out_0;
            }
            int x;
            if (buf == "1")
            {
                nplayers = 1;
                x = 1;
            }
            else
            {
                if (buf == "2")
                {
                    nplayers = 2;
                    x = 1;
                }
                else
                {
                    if (buf[0] == 'M')
                    {
                        diggers = buf[1] - '0';
                        x = 2;
                    }
                    else
                    {
                        x = 0;
                    }
                    if (buf[x] == 'G')
                    {
                        gauntlet = true;
                        x++;
                        gtime = uint.Parse(buf.Substring(x));
                        while (buf[x] >= '0' && buf[x] <= '9')
                            x++;
                    }
                }
            }
            if (buf[x] == 'U') /* Unlimited lives are ignored on playback. */
                x++;
            if (buf[x] == 'I')
                startlev = int.Parse(buf.Substring(x + 1));
            /* Get bonus score */
            if ((buf = smart_fgets(playf)) == null)
            {
                goto out_0;
            }
            bonusscore = int.Parse(buf);
            for (int n = 0; n < 8; n++)
                for (int y = 0; y < 10; y++)
                {
                    /* Get a line of map */
                    if ((buf = smart_fgets(playf)) == null)
                    {
                        goto out_0;
                    }
                    leveldat[n, y] = buf;
                }

            /* This is the second. The line breaks here really are only so that the file
               can be emailed. */
            int l = (int)(playf.Length - playf.Position);
            plb = plp = new char[l];

            for (int i = 0, j = 0; i < l; i++)
            {
                char c = (char)playf.ReadByte(); /* Get everything that isn't line break into 1 string */
                if (c >= ' ')
                    plp[j++] = c;
            }
            playf.Close();
            plp = plb;

            playing = true;
            recinit();
            game();
            gotgame = true;
            playing = false;
            gauntlet = origg;
            gtime = origgtime;
            kludge = false;
            startlev = origstartlev;
            diggers = origdiggers;
            nplayers = orignplayers;
            return;
            out_0:
            if (playf != null)
                playf.Close();
            escape = true;
        }

        public static void recstart()
        {
            recb = new char[MAX_REC_BUFFER];
            recp = 0;
        }

        public static void mprintf(string format, params object[] args)
        {
            string buf = string.Format(format, args);
            for (int i = 0; i < buf.Length; i++)
                recb[recp + i] = buf[i];
            recp += (uint)buf.Length;
            if (recp > MAX_REC_BUFFER - buf.Length)
                recp = 0;          /* Give up, file is too long */
        }

        public static void makedir(ref int dir, ref bool fire, char d)
        {
            if (d >= 'A' && d <= 'Z')
            {
                fire = true;
                d -= (char)A_minus_a;
            }
            else
                fire = false;
            switch (d)
            {
                case 's': dir = DIR_NONE; break;
                case 'r': dir = DIR_RIGHT; break;
                case 'u': dir = DIR_UP; break;
                case 'l': dir = DIR_LEFT; break;
                case 'd': dir = DIR_DOWN; break;
            }
        }

        public static void playgetdir(ref int dir, ref bool fire)
        {
            if (rlleft > 0)
            {
                makedir(ref dir, ref fire, rld);
                rlleft--;
            }
            else
            {
                for (int i = 0; i < plp.Length; ++i)
                {
                    if (plp[i] == 'E' || plp[i] == 'e')
                    {
                        escape = true;
                        return;
                    }
                    rld = plp[i];
                    while (plp[i] >= '0' && plp[i] <= '9')
                        rlleft = rlleft * 10 + (plp[++i]) - '0';
                }
                makedir(ref dir, ref fire, rld);
                if (rlleft > 0)
                    rlleft--;
            }
        }

        public static char maked(int dir, bool fire)
        {
            char d;
            if (dir == DIR_NONE)
                d = 's';
            else
                d = "ruld"[dir >> 1];
            if (fire)
                d += (char)A_minus_a;

            return d;
        }

        public static void putrun()
        {
            if (recrl > 1)
                mprintf("{0}{1:d}", recd, recrl);
            else
                mprintf("{0}", recd);
            reccc++;
            if (recrl > 1)
            {
                reccc++;
                if (recrl >= 10)
                {
                    reccc++;
                    if (recrl >= 100)
                        reccc++;
                }
            }
            if (reccc >= 60)
            {
                mprintf("\n");
                reccc = 0;
            }
        }

        public static void recputdir(int dir, bool fire)
        {
            char d = maked(dir, fire);
            if (recrl == 0)
                recd = d;
            if (recd != d)
            {
                putrun();
                recd = d;
                recrl = 1;
            }
            else
            {
                if (recrl == 999)
                {
                    putrun(); /* This probably won't ever happen. */
                    recrl = 0;
                }
                recrl++;
            }
        }

        public static void recinit()
        {
            recp = 0;
            drfvalid = true;

            mprintf("DRF\n"); /* Required at start of DRF */
            if (kludge)
                mprintf("AJ DOS 19981125\n");
            else
                mprintf(DIGGER_VERSION + "\n");
            if (diggers > 1)
            {
                mprintf("M{0}", diggers);
                if (gauntlet)
                    mprintf("G{0}", gtime);
            }
            else
              if (gauntlet)
                mprintf("G{0}", gtime);
            else
                mprintf("{0}", nplayers);
            /*  if (unlimlives)
                mprintf("U"); */
            if (startlev > 1)
                mprintf("I{0}", startlev);
            mprintf("\n{0}\n", bonusscore);
            for (int l = 0; l < 8; l++)
            {
                for (int y = 0; y < MHEIGHT; y++)
                {
                    for (int x = 0; x < MWIDTH; x++)
                        mprintf("{0}", leveldat[l, y][x]);
                    mprintf("\n");
                }
            }
            reccc = recrl = 0;
        }

        public static void recputrand(uint randv)
        {
            mprintf("{0:X8}\n", randv);
            reccc = recrl = 0;
        }

        public static void recsavedrf()
        {
            if (!drfvalid)
                return;
            FileStream recf = null;
            try
            {
                if (gotname)
                {
                    try
                    {
                        recf = File.OpenWrite(rname);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                        gotname = false;
                    }
                }
                if (!gotname)
                {
                    if (nplayers == 2)
                        recf = File.OpenWrite(DEFAULTSN); /* Should get a name, really */
                    else
                    {
                        char[] init = new char[4];
                        for (int j = 0; j < 3; j++)
                        {
                            init[j] = scoreinit[0][j];
                            if (!((init[j] >= 'A' && init[j] <= 'Z') ||
                                  (init[j] >= 'a' && init[j] <= 'z')))
                                init[j] = '_';
                        }
                        init[3] = '\0';
                        string nambuf;
                        if (scoret < 100000)
                            nambuf = $"{init}{scoret}";
                        else if (init[2] == '_')
                            nambuf = $"{init[0]}{init[1]}{scoret}";
                        else if (init[0] == '_')
                            nambuf = $"{init[1]}{init[2]}{scoret}";
                        else
                            nambuf = $"{init[0]}{init[2]}{scoret}";
                        nambuf += ".drf";
                        recf = File.OpenWrite(nambuf);
                    }
                }

                for (int i = 0; i < recp; i++)
                    recf.WriteByte((byte)recb[i]);
            }
            finally
            {
                if (recf != null)
                    recf.Close();
            }
        }

        public static void playskipeol()
        {
            var tmp = new char[plp.Length - 3];
            Buffer.BlockCopy(plp, 3, tmp, 0, tmp.Length);
            plp = tmp;
        }

        public static uint playgetrand()
        {
            uint r = 0;
            char p;
            int offset = 0;
            if (plp[offset] == '*')
                offset += 4;
            for (int i = 0; i < 8; i++)
            {
                p = plp[offset + i];
                if (p >= '0' && p <= '9')
                    r |= (uint)(p - '0') << ((7 - i) << 2);
                if (p >= 'A' && p <= 'F')
                    r |= (uint)(p - 'A' + 10) << ((7 - i) << 2);
                if (p >= 'a' && p <= 'f')
                    r |= (uint)(p - 'a' + 10) << ((7 - i) << 2);
            }
            return r;
        }

        public static void recputinit(string init)
        {
            mprintf("*{0}{1}{2}\n", init[0], init[1], init[2]);
        }

        public static void recputeol()
        {
            if (recrl > 0)
                putrun();
            if (reccc > 0)
                mprintf("\n");
            mprintf("EOL\n");
        }

        public static void recputeog()
        {
            mprintf("EOG\n");
        }

        public static void recname(string name)
        {
            gotname = true;
            rname = name;
        }
    }
}