namespace Digger.Net
{
    public static class Const
    {
        public const int DIR_NONE = -1;
        public const int DIR_RIGHT = 0;
        public const int DIR_UP = 2;
        public const int DIR_LEFT = 4;
        public const int DIR_DOWN = 6;

        public const int TYPES = 5;
        public const int SPRITES = (BONUSES + BAGS + MONSTERS + FIREBALLS + DIGGERS);
        public const int BONUSES = 1;
        public const int BAGS = 7;
        public const int MONSTERS = 6;
        public const int FIREBALLS = DIGGERS;
        public const int DIGGERS = 2;

        public const int MAX_W = 320;
        public const int MAX_H = 200;
        public const int CHR_W = 12;
        public const int CHR_H = 12;

        public const int MAX_TEXT_LEN = MAX_W / CHR_W + 1;

        public const int MWIDTH = 15;
        public const int MHEIGHT = 10;
        public const int MSIZE = MWIDTH * MHEIGHT;

        public const int DEFAULT_FRAME_TIME = 80000;
        public const int DEFAULT_GAUNTLET_TIME = 120;

        /* Sprite order is figured out here. By LAST I mean last+1. */
        public const int FIRSTBONUS = 0;
        public const int LASTBONUS = (FIRSTBONUS + BONUSES);
        public const int FIRSTBAG = LASTBONUS;
        public const int LASTBAG = (FIRSTBAG + BAGS);
        public const int FIRSTMONSTER = LASTBAG;
        public const int LASTMONSTER = (FIRSTMONSTER + MONSTERS);
        public const int FIRSTFIREBALL = LASTMONSTER;
        public const int LASTFIREBALL = (FIRSTFIREBALL + FIREBALLS);
        public const int FIRSTDIGGER = LASTFIREBALL;
        public const int LASTDIGGER = (FIRSTDIGGER + DIGGERS);

        /// <summary>
        /// Version string:
        /// First word: your initials if you have changed anything.
        /// Second word: platform. 
        /// Third word: compilation date in yyyymmdd format.
        /// </summary>
        public const string DIGGER_VERSION = "MS SDL 20180419";
    }
}
