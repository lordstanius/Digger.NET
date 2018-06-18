/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

using System.IO;
using System.Text;

namespace Digger.Source
{
    public class Level
    {
        private readonly Game game;
        public string levelFileName;
        public bool isUsingLevelFile;
        public readonly string[,] Data = {
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

        public Level(Game game)
        {
            this.game = game;
        }

        public static int LevelOf10(int level)
        {
            return level > 10 ? 10 : level;
        }

        public char GetLevelChar(int x, int y)
        {
            int l = LevelPlan();
            if ((l == 3 || l == 4) && !isUsingLevelFile && game.diggerCount == 2 && y == 9 && (x == 6 || x == 8))
                return 'H';

            return Data[l - 1, y][x];
        }

        public int LevelPlan()
        {
            int l = game.Level;
            if (l > 8)
                return (l & 3) + 5; /* Level plan: 12345678, 678, (5678) 247 times, 5 forever */

            return l;
        }

        public void ReadLevelFile(ref int bonusScore)
        {
            if (!File.Exists(levelFileName))
            {
                levelFileName += ".DLF";
                if (!File.Exists(levelFileName))
                    throw new FileNotFoundException($"File '{levelFileName}' cannot be found.");
            }

            using (var levf = File.OpenRead(levelFileName))
            {
                using (var br = new BinaryReader(levf, Encoding.ASCII, true))
                {
                    bonusScore = br.ReadInt32();
                }

                byte[] buff = new byte[15];
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        levf.Read(buff, 0, 15);
                        Data[i, j] = Encoding.ASCII.GetString(buff);
                    }
                }
            }
        }
    }
}
