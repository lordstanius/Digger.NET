/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Digger.Source
{
    public class Recorder
    {
        private const string REC_FILE_NAME = "DiggerGameRecord";
        private const string REC_FILE_EXT = ".drf";

        public bool isPlaying;
        public bool saveDrf;
        public bool gotName;
        public bool isDrfValid;
        public bool kludge;

        private int recordCharCount;
        private int recordRunLenght;
        private int rlleft;
        private char recd, rld;

#if INTDRF
        private FileStream info;
#endif

        private string playBuffer;
        private string recordName;
        private StringBuilder recordingBuffer = new StringBuilder();
        private readonly int A_minus_a = 'A' - 'a';

        private readonly Game game;

        public Recorder(Game game)
        {
            this.game = game;
        }

        public void OpenPlay(string name)
        {
            if (!File.Exists(name))
            {
                name += REC_FILE_EXT;
                if (!File.Exists(name))
                    throw new FileNotFoundException($"File '{name}' cannot be found.");
            }

            // save current values
            uint origgtime = game.gameTime;
            bool origg = game.isGauntletMode;
            int origstartlev = game.startingLevel;
            int orignplayers = game.playerCount;
            int origdiggers = game.diggerCount;

            using (var playf = new StreamReader(name, Encoding.ASCII))
            {
#if INTDRF
                info = File.OpenWrite("DRFINFO.TXT");
#endif
                game.isGauntletMode = false;
                game.startingLevel = 1;
                game.playerCount = 1;
                game.diggerCount = 1;
                /* The file is in two distinct parts. In the first, line breaks are used as
                   separators. In the second, they are ignored. This is the first. */

                /* Get id string */
                string buf;
                if ((buf = playf.ReadLine()) == null)
                    throw new FileLoadException("File is empty");

                if (!buf.StartsWith("DRF"))
                    throw new InvalidOperationException("File content has incorrect format.");

                /* Get version for kludge switches */
                if ((buf = playf.ReadLine()) == null)
                    throw new InvalidOperationException("No content.");

                if (int.Parse(buf.Substring(7)) <= 19981125)
                    kludge = true;

                /* Get mode */
                if ((buf = playf.ReadLine()) == null)
                    throw new InvalidOperationException("Cannot read mode.");

                if (buf.StartsWith("1"))
                {
                    game.playerCount = 1;
                    buf = buf.Substring(1);
                }
                else
                {
                    if (buf.StartsWith("2"))
                    {
                        game.playerCount = 2;
                        buf = buf.Substring(1);
                    }
                    else
                    {
                        if (buf.StartsWith("M"))
                        {
                            game.diggerCount = buf[1] - '0';
                            buf = buf.Substring(2);
                        }

                        if (buf.StartsWith("G"))
                        {
                            game.isGauntletMode = true;
                            game.gameTime = uint.Parse(buf.Substring(1));
                            while (char.IsDigit(buf[0]))
                                buf = buf.Substring(1);
                        }
                    }
                }
                if (buf.StartsWith("U")) /* Unlimited lives are ignored on playback. */
                    buf = buf.Substring(1);

                if (buf.StartsWith("I"))
                    game.startingLevel = int.Parse(buf.Substring(1));

                /* Get bonus score */
                if ((buf = playf.ReadLine()) == null)
                    throw new InvalidOperationException("Cannot read bonus score.");

                game.scores.bonusscore = int.Parse(buf);
                for (int n = 0; n < 8; n++)
                    for (int y = 0; y < 10; y++)
                    {
                        /* Get a line of map */
                        if ((buf = playf.ReadLine()) == null)
                            throw new InvalidOperationException("Cannot read a line of the map.");

                        Level.Data[n, y] = buf;
                    }

                /* This is the second. The line breaks here really are only so that the file
                   can be emailed. */
                playBuffer = new string(playf.ReadToEnd().Where(c => c >= ' ').ToArray());
            }

            isPlaying = true;

            game.Run();
            
            isPlaying = false;

            // restore current values
            game.isGauntletMode = origg;
            game.gameTime = origgtime;
            kludge = false;
            game.startingLevel = origstartlev;
            game.diggerCount = origdiggers;
            game.playerCount = orignplayers;
        }

        public void MakeDirection(ref int dir, ref bool fire, char d)
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
                case 's': dir = Dir.None; break;
                case 'r': dir = Dir.Right; break;
                case 'u': dir = Dir.Up; break;
                case 'l': dir = Dir.Left; break;
                case 'd': dir = Dir.Down; break;
            }
        }

        public void PlayGetDirection(ref int dir, ref bool fire)
        {
            if (rlleft > 0)
            {
                MakeDirection(ref dir, ref fire, rld);
                rlleft--;
            }
            else
            {
                if (playBuffer[0] == 'E' || playBuffer[0] == 'e')
                {
                    game.isGameCycleEnded = true;
                    return;
                }

                rld = playBuffer[0];
                playBuffer = playBuffer.Substring(1);

                int i = 0;
                while (char.IsDigit(playBuffer[i]))
                    rlleft = rlleft * 10 + playBuffer[i++] - '0';

                playBuffer = playBuffer.Substring(i);

                MakeDirection(ref dir, ref fire, rld);
                if (rlleft > 0)
                    rlleft--;
            }
        }

        public char MakeDirection(int dir, bool fire)
        {
            char d;
            if (dir == Dir.None)
                d = 's';
            else
                d = "ruld"[dir >> 1];
            if (fire)
                d += (char)A_minus_a;

            return d;
        }

        public void PutRun()
        {
            if (recordRunLenght > 1)
                recordingBuffer.AppendFormat("{0}{1:d}", recd, recordRunLenght);
            else
                recordingBuffer.AppendFormat("{0}", recd);

            recordCharCount++;
            if (recordRunLenght > 1)
            {
                recordCharCount++;
                if (recordRunLenght >= 10)
                {
                    recordCharCount++;
                    if (recordRunLenght >= 100)
                        recordCharCount++;
                }
            }

            if (recordCharCount >= 60)
            {
                recordingBuffer.AppendLine();
                recordCharCount = 0;
            }
        }

        public void PutDirection(int dir, bool fire)
        {
            char d = MakeDirection(dir, fire);
            if (recordRunLenght == 0)
                recd = d;

            if (recd != d)
            {
                PutRun();
                recd = d;
                recordRunLenght = 1;
            }
            else
            {
                if (recordRunLenght == 999)
                {
                    PutRun(); /* This probably won't ever happen. */
                    recordRunLenght = 0;
                }
                recordRunLenght++;
            }
        }

        public void StartRecording()
        {
            recordingBuffer.Clear();
            isDrfValid = true;

            recordingBuffer.AppendLine("DRF"); /* Required at start of DRF */
            if (kludge)
                recordingBuffer.AppendLine("AJ DOS 19981125");
            else
                recordingBuffer.AppendLine(Const.DIGGER_VERSION);

            if (game.diggerCount > 1)
            {
                recordingBuffer.AppendFormat("M{0}", game.diggerCount);
                if (game.isGauntletMode)
                    recordingBuffer.AppendFormat("G{0}", game.gameTime);
            }
            else if (game.isGauntletMode)
            {
                recordingBuffer.AppendFormat("G{0}", game.gameTime);
            }
            else
            {
                recordingBuffer.AppendFormat("{0}", game.playerCount);
            }

            /*  if (unlimlives)
                mprintf("U"); */
            if (game.startingLevel > 1)
                recordingBuffer.AppendFormat("I{0}", game.startingLevel);

            recordingBuffer.AppendFormat("\n{0}\n", game.scores.bonusscore);
            for (int lvl = 0; lvl < 8; lvl++)
            {
                for (int y = 0; y < Const.MHEIGHT; y++)
                {
                    for (int x = 0; x < Const.MWIDTH; x++)
                        recordingBuffer.AppendFormat("{0}", Level.Data[lvl, y][x]);

                    recordingBuffer.AppendLine();
                }
            }

            recordCharCount = recordRunLenght = 0;
        }

        public void RecordPutRandom(int randv)
        {
            recordingBuffer.AppendFormat("{0:X8}\n", randv);
            recordCharCount = recordRunLenght = 0;
        }

        public void SaveRecordFile()
        {
            if (!isDrfValid)
                return;

            FileStream recf = null;
            try
            {
                if (gotName)
                {
                    try
                    {
                        recf = File.OpenWrite(recordName);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex);
                        gotName = false;
                    }
                }

                if (!gotName)
                {
                    if (game.playerCount == 2)
                        recf = File.OpenWrite(REC_FILE_NAME); /* Should get a name, really */
                    else
                    {
                        char[] initials = "___".ToCharArray();
                        if (game.scores.scoreinit[0] == null)
                        {
                            initials = "rec".ToCharArray();
                        }
                        else
                        {
                            for (int j = 0; j < initials.Length; j++)
                            {
                                initials[j] = game.scores.scoreinit[0][j];
                                if (!((initials[j] >= 'A' && initials[j] <= 'Z') ||
                                      (initials[j] >= 'a' && initials[j] <= 'z')))
                                    initials[j] = '_';
                            }
                        }

                        string fileName = string.Format("{0}{1}{2}", new String(initials), game.scores.scoret, REC_FILE_EXT);
                        recf = File.OpenWrite(fileName);
                    }
                }

                byte[] recordedBytes = Encoding.ASCII.GetBytes(recordingBuffer.ToString());
                recf.Write(recordedBytes, 0, recordingBuffer.Length);
            }
            finally
            {
                if (recf != null)
                    recf.Close();
            }
        }

        public void PlaySkipEOL()
        {
            playBuffer = playBuffer.Substring(3);
        }

        public int PlayGetRand()
        {
            int rand = 0;
            int offset = 0;
            if (playBuffer[offset] == '*')
                offset += 4;

            for (int i = 0; i < 8; i++)
            {
                char p = playBuffer[offset++];
                if (p >= '0' && p <= '9')
                    rand |= (p - '0') << ((7 - i) << 2);
                if (p >= 'A' && p <= 'F')
                    rand |= (p - 'A' + 10) << ((7 - i) << 2);
                if (p >= 'a' && p <= 'f')
                    rand |= (p - 'a' + 10) << ((7 - i) << 2);
            }

            playBuffer = playBuffer.Substring(offset);
            return rand;
        }

        public void PutInitials(string init)
        {
            recordingBuffer.AppendFormat("*{0}{1}{2}\n", init[0], init[1], init[2]);
        }

        public void PutEndOfLevel()
        {
            if (recordRunLenght > 0)
                PutRun();

            if (recordCharCount > 0)
                recordingBuffer.Append('\n');

            recordingBuffer.Append("EOL\n");
        }

        public void PutEndOfGame()
        {
            recordingBuffer.Append("EOG\n");
        }

        public void SetRecordName(string name)
        {
            gotName = true;
            if (!Path.HasExtension(name))
                name += REC_FILE_EXT;

            recordName = name;
        }
    }
}