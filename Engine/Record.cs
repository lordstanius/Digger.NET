/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

using System;
using System.IO;

namespace Digger.Net
{
    public class Record
    {
        public const string DEFAULTSN = "DRF";
        public const int MAX_REC_BUFFER = 262144;

        readonly int A_minus_a = 'A' - 'a';
        /* I reckon this is enough for about 36 hours of continuous play. */

        public char[] recb, plb, plp;

        public bool playing, savedrf, gotname, gotgame, drfvalid, kludge;

        public string rname;

        public int reccc, recrl, rlleft;
        public uint recp;
        public char recd, rld;

#if INTDRF
        private  FileStream info;
#endif

        string smart_fgets(FileStream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadLine();
        }

        public void openplay(string name)
        {
            FileStream playf;
            try
            {
                playf = File.OpenRead(name);
            }
            catch (Exception ex)
            {
                DebugLog.Write(ex);
                DiggerC.input.escape = true;
                return;
            }

            uint origgtime = DiggerC.g_gameTime;
            bool origg = DiggerC.g_isGauntletMode;
            int origstartlev = DiggerC.g_StartingLevel, orignplayers = DiggerC.g_playerCount, origdiggers = DiggerC.g_Diggers;
# if INTDRF
            info = File.OpenWrite("DRFINFO.TXT");
#endif
            DiggerC.g_isGauntletMode = false;
            DiggerC.g_StartingLevel = 1;
            DiggerC.g_playerCount = 1;
            DiggerC.g_Diggers = 1;
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
                DiggerC.g_playerCount = 1;
                x = 1;
            }
            else
            {
                if (buf == "2")
                {
                    DiggerC.g_playerCount = 2;
                    x = 1;
                }
                else
                {
                    if (buf[0] == 'M')
                    {
                        DiggerC.g_Diggers = buf[1] - '0';
                        x = 2;
                    }
                    else
                    {
                        x = 0;
                    }
                    if (buf[x] == 'G')
                    {
                        DiggerC.g_isGauntletMode = true;
                        x++;
                        DiggerC.g_gameTime = uint.Parse(buf.Substring(x));
                        while (buf[x] >= '0' && buf[x] <= '9')
                            x++;
                    }
                }
            }
            if (buf[x] == 'U') /* Unlimited lives are ignored on playback. */
                x++;
            if (buf[x] == 'I')
                DiggerC.g_StartingLevel = int.Parse(buf.Substring(x + 1));
            /* Get bonus score */
            if ((buf = smart_fgets(playf)) == null)
            {
                goto out_0;
            }
            DiggerC.scores.bonusscore = int.Parse(buf);
            for (int n = 0; n < 8; n++)
                for (int y = 0; y < 10; y++)
                {
                    /* Get a line of map */
                    if ((buf = smart_fgets(playf)) == null)
                    {
                        goto out_0;
                    }
                    DiggerC.level.leveldat[n, y] = buf;
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
            DiggerC.game();
            gotgame = true;
            playing = false;
            DiggerC.g_isGauntletMode = origg;
            DiggerC.g_gameTime = origgtime;
            kludge = false;
            DiggerC.g_StartingLevel = origstartlev;
            DiggerC.g_Diggers = origdiggers;
            DiggerC.g_playerCount = orignplayers;
            return;
            out_0:
            if (playf != null)
                playf.Close();
            DiggerC.input.escape = true;
        }

        public void recstart()
        {
            recb = new char[MAX_REC_BUFFER];
            recp = 0;
        }

        public void mprintf(string format, params object[] args)
        {
            string buf = string.Format(format, args);
            for (int i = 0; i < buf.Length; i++)
                recb[recp + i] = buf[i];
            recp += (uint)buf.Length;
            if (recp > MAX_REC_BUFFER - buf.Length)
                recp = 0;          /* Give up, file is too long */
        }

        public void makedir(ref int dir, ref bool fire, char d)
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
                case 's': dir = DiggerC.DIR_NONE; break;
                case 'r': dir = DiggerC.DIR_RIGHT; break;
                case 'u': dir = DiggerC.DIR_UP; break;
                case 'l': dir = DiggerC.DIR_LEFT; break;
                case 'd': dir = DiggerC.DIR_DOWN; break;
            }
        }

        public void playgetdir(ref int dir, ref bool fire)
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
                        DiggerC.input.escape = true;
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

        public char maked(int dir, bool fire)
        {
            char d;
            if (dir == DiggerC.DIR_NONE)
                d = 's';
            else
                d = "ruld"[dir >> 1];
            if (fire)
                d += (char)A_minus_a;

            return d;
        }

        public void putrun()
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

        public void recputdir(int dir, bool fire)
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

        public void recinit()
        {
            recp = 0;
            drfvalid = true;

            mprintf("DRF\n"); /* Required at start of DRF */
            if (kludge)
                mprintf("AJ DOS 19981125\n");
            else
                mprintf(DiggerC.DIGGER_VERSION + "\n");
            if (DiggerC.g_Diggers > 1)
            {
                mprintf("M{0}", DiggerC.g_Diggers);
                if (DiggerC.g_isGauntletMode)
                    mprintf("G{0}", DiggerC.g_gameTime);
            }
            else
              if (DiggerC.g_isGauntletMode)
                mprintf("G{0}", DiggerC.g_gameTime);
            else
                mprintf("{0}", DiggerC.g_playerCount);
            /*  if (unlimlives)
                mprintf("U"); */
            if (DiggerC.g_StartingLevel > 1)
                mprintf("I{0}", DiggerC.g_StartingLevel);
            mprintf("\n{0}\n", DiggerC.scores.bonusscore);
            for (int l = 0; l < 8; l++)
            {
                for (int y = 0; y < DiggerC.MHEIGHT; y++)
                {
                    for (int x = 0; x < DiggerC.MWIDTH; x++)
                        mprintf("{0}", DiggerC.level.leveldat[l, y][x]);
                    mprintf("\n");
                }
            }
            reccc = recrl = 0;
        }

        public void RecordPutRandom(uint randv)
        {
            mprintf("{0:X8}\n", randv);
            reccc = recrl = 0;
        }

        public void recsavedrf()
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
                        DebugLog.Write(ex);
                        gotname = false;
                    }
                }
                if (!gotname)
                {
                    if (DiggerC.g_playerCount == 2)
                        recf = File.OpenWrite(DEFAULTSN); /* Should get a name, really */
                    else
                    {
                        char[] init = new char[4];
                        for (int j = 0; j < 3; j++)
                        {
                            init[j] = DiggerC.scores.scoreinit[0][j];
                            if (!((init[j] >= 'A' && init[j] <= 'Z') ||
                                  (init[j] >= 'a' && init[j] <= 'z')))
                                init[j] = '_';
                        }
                        init[3] = '\0';
                        string nambuf;
                        if (DiggerC.scores.scoret < 100000)
                            nambuf = string.Format("{0}{1}", init[0], DiggerC.scores.scoret);
                        else if (init[2] == '_')
                            nambuf = string.Format("{0}{1}{2}", init[0], init[1], DiggerC.scores.scoret);
                        else if (init[0] == '_')
                            nambuf = string.Format("{0}{1}{2}", init[1], init[2], DiggerC.scores.scoret);
                        else
                            nambuf = string.Format("{0}{1}{2}", init[0], init[2], DiggerC.scores.scoret);
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

        public void playskipeol()
        {
            var tmp = new char[plp.Length - 3];
            Buffer.BlockCopy(plp, 3, tmp, 0, tmp.Length);
            plp = tmp;
        }

        public uint playgetrand()
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

        public void recputinit(string init)
        {
            mprintf("*{0}{1}{2}\n", init[0], init[1], init[2]);
        }

        public void recputeol()
        {
            if (recrl > 0)
                putrun();
            if (reccc > 0)
                mprintf("\n");
            mprintf("EOL\n");
        }

        public void recputeog()
        {
            mprintf("EOG\n");
        }

        public void recname(string name)
        {
            gotname = true;
            rname = name;
        }
    }
}