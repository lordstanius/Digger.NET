using System;
using System.IO;
using System.Text;

namespace Digger.Net
{
    public class Level
    {
        public string levfname;
        public bool levfflag = false;

        public readonly string[,] leveldat = {
            { "S   B     HHHHS",
              "V  CC  C  V B  ",
              "VB CC  C  V    ",
              "V  CCB CB V CCC",
              "V  CC  C  V CCC",
              "HH CC  C  V CCC",
              " V    B B V    ",
              " HHHH     V    ",
              "C   V     V   C",
              "CC  HHHHHHH  CC" },
            { "SHHHHH  B B  HS",
              " CC  V       V ",
              " CC  V CCCCC V ",
              "BCCB V CCCCC V ",
              "CCCC V       V ",
              "CCCC V B  HHHH ",
              " CC  V CC V    ",
              " BB  VCCCCV CC ",
              "C    V CC V CC ",
              "CC   HHHHHH    "},
            { "SHHHHB B BHHHHS",
              "CC  V C C V BB ",
              "C   V C C V CC ",
              " BB V C C VCCCC",
              "CCCCV C C VCCCC",
              "CCCCHHHHHHH CC ",
              " CC  C V C  CC ",
              " CC  C V C     ",
              "C    C V C    C",
              "CC   C H C   CC"},
            { "SHBCCCCBCCCCBHS",
              "CV  CCCCCCC  VC",
              "CHHH CCCCC HHHC",
              "C  V  CCC  V  C",
              "   HHH C HHH   ",
              "  B  V B V  B  ",
              "  C  VCCCV  C  ",
              " CCC HHHHH CCC ",
              "CCCCC CVC CCCCC",
              "CCCCC CHC CCCCC"},
            { "SHHHHHHHHHHHHHS",
              "VBCCCCBVCCCCCCV",
              "VCCCCCCV CCBC V",
              "V CCCC VCCBCCCV",
              "VCCCCCCV CCCC V",
              "V CCCC VBCCCCCV",
              "VCCBCCCV CCCC V",
              "V CCBC VCCCCCCV",
              "VCCCCCCVCCCCCCV",
              "HHHHHHHHHHHHHHH"},
            { "SHHHHHHHHHHHHHS",
              "VCBCCV V VCCBCV",
              "VCCC VBVBV CCCV",
              "VCCCHH V HHCCCV",
              "VCC V CVC V CCV",
              "VCCHH CVC HHCCV",
              "VC V CCVCC V CV",
              "VCHHBCCVCCBHHCV",
              "VCVCCCCVCCCCVCV",
              "HHHHHHHHHHHHHHH"},
            { "SHCCCCCVCCCCCHS",
              " VCBCBCVCBCBCV ",
              "BVCCCCCVCCCCCVB",
              "CHHCCCCVCCCCHHC",
              "CCV CCCVCCC VCC",
              "CCHHHCCVCCHHHCC",
              "CCCCV CVC VCCCC",
              "CCCCHH V HHCCCC",
              "CCCCCV V VCCCCC",
              "CCCCCHHHHHCCCCC"},
            { "HHHHHHHHHHHHHHS",
              "V CCBCCCCCBCC V",
              "HHHCCCCBCCCCHHH",
              "VBV CCCCCCC VBV",
              "VCHHHCCCCCHHHCV",
              "VCCBV CCC VBCCV",
              "VCCCHHHCHHHCCCV",
              "VCCCC V V CCCCV",
              "VCCCCCV VCCCCCV",
              "HHHHHHHHHHHHHHH" }
        };

        private game_data[] gameData;

        public Level(game_data[] gameData)
        {
            this.gameData = gameData;
        }

        public char getlevch(int x, int y, int l)
        {
            if ((l == 3 || l == 4) && !levfflag && DiggerC.g_Diggers == 2 && y == 9 && (x == 6 || x == 8))
                return 'H';
            return leveldat[l - 1, y][x];
        }

        public int levplan()
        {
            int l = levno();
            if (l > 8)
                l = (l & 3) + 5; /* Level plan: 12345678, 678, (5678) 247 times, 5 forever */
            return l;
        }

        public int levof10()
        {
            if (gameData[DiggerC.g_CurrentPlayer].level > 10)
                return 10;
            return gameData[DiggerC.g_CurrentPlayer].level;
        }

        public int levno()
        {
            return gameData[DiggerC.g_CurrentPlayer].level;
        }

        public int read_levf()
        {
            FileStream levf = null;
            try
            {
                try
                {
                    levf = File.OpenRead(levfname);
                }
                catch (FileNotFoundException)
                {
                    levfname += ".DLF";
                    try
                    {
                        levf = File.OpenRead(levfname);
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write(ex);
                        DebugLog.Write("read_levf: levels file open error:");
                        return (-1);
                    }
                }

                using (var br = new BinaryReader(levf))
                {
                    try
                    {
                        DiggerC.bonusscore = br.ReadInt32();
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write("read_levf: levels load error 1");
                        DebugLog.Write(ex);
                        return -1;
                    }
                }

                try
                {
                    byte[] buff = new byte[15];
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            levf.Read(buff, 0, 15);
                            leveldat[i, j] = Encoding.ASCII.GetString(buff);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.Write("read_levf: levels load error 2");
                    DebugLog.Write(ex);
                    return -1;
                }
            }
            finally
            {
                if (levf != null)
                    levf.Close();
            }

            return 0;
        }
    }
}
