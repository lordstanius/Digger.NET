namespace Digger.Net
{
    public class Emeralds
    {
        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int MSIZE = Const.MSIZE;

        private const int DIR_NONE = Const.DIR_NONE;
        private const int DIR_RIGHT = Const.DIR_RIGHT;
        private const int DIR_UP = Const.DIR_UP;
        private const int DIR_LEFT = Const.DIR_LEFT;
        private const int DIR_DOWN = Const.DIR_DOWN;

        private int emmask = 0;
        private readonly short[] emeraldBox = { 8, 12, 12, 9, 16, 12, 6, 9 };
        private readonly byte[] emeraldField = new byte[MSIZE];

        private Game game;
        private Video video;

        public Emeralds(Game game)
        {
            this.game = game;
            this.video = game.video;
        }

        public void DrawEmeralds()
        {
            emmask = (short)(1 << game.currentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emeraldField[y * MWIDTH + x] & emmask) != 0)
                        video.DrawEmerald(x * 20 + 12, y * 18 + 21);
        }

        public void MakeEmeraldField()
        {
            emmask = (short)(1 << game.currentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (game.level.GetLevelChar(x, y, game.level.LevelPlan()) == 'C')
                        emeraldField[y * MWIDTH + x] |= (byte)emmask;
                    else
                        emeraldField[y * MWIDTH + x] &= (byte)~emmask;
        }

        public bool HitEmerald(int x, int y, int rx, int ry, int dir)
        {
            if (dir != DIR_RIGHT && dir != DIR_UP && dir != DIR_LEFT && dir != DIR_DOWN)
                return false;

            if (dir == DIR_RIGHT && rx != 0)
                x++;

            if (dir == DIR_DOWN && ry != 0)
                y++;

            int r;
            if (dir == DIR_RIGHT || dir == DIR_LEFT)
                r = rx;
            else
                r = ry;

            if ((emeraldField[y * MWIDTH + x] & emmask) != 0)
            {
                if (r == emeraldBox[dir])
                {
                    video.DrawEmerald(x * 20 + 12, y * 18 + 21);
                    game.IncreasePenalty();
                }
                if (r == emeraldBox[dir + 1])
                {
                    video.EraseEmerald(x * 20 + 12, y * 18 + 21);
                    game.IncreasePenalty();
                    emeraldField[y * MWIDTH + x] &= (byte)~emmask;
                    return true;
                }
            }

            return false;
        }

        public int Count()
        {
            int n = 0;
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emeraldField[y * MWIDTH + x] & emmask) != 0)
                        n++;
            return n;
        }

        public void KillEmerald(int x, int y)
        {
            if ((emeraldField[(y + 1) * MWIDTH + x] & emmask) != 0)
            {
                emeraldField[(y + 1) * MWIDTH + x] &= (byte)~emmask;
                video.EraseEmerald(x * 20 + 12, (y + 1) * 18 + 21);
            }
        }
    }
}
