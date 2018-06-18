namespace Digger.Source
{
    public class Emeralds
    {
        private const int MWIDTH = Const.MWIDTH;
        private const int MHEIGHT = Const.MHEIGHT;
        private const int MSIZE = Const.MSIZE;

        private int emmask = 0;
        private readonly short[] emeraldBox = { 8, 12, 12, 9, 16, 12, 6, 9 };
        private readonly byte[] emeraldField = new byte[MSIZE];

        private Game game;

        public Emeralds(Game game)
        {
            this.game = game;
        }

        public void DrawEmeralds()
        {
            emmask = (short)(1 << game.currentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if ((emeraldField[y * MWIDTH + x] & emmask) != 0)
                        game.drawing.DrawEmerald(x * 20 + 12, y * 18 + 21);
        }

        public void MakeEmeraldField()
        {
            emmask = (short)(1 << game.currentPlayer);
            for (int x = 0; x < MWIDTH; x++)
                for (int y = 0; y < MHEIGHT; y++)
                    if (game.level.GetLevelChar(x, y) == 'C')
                        emeraldField[y * MWIDTH + x] |= (byte)emmask;
                    else
                        emeraldField[y * MWIDTH + x] &= (byte)~emmask;
        }

        public bool IsEmeraldHit(int x, int y, int rx, int ry, int dir)
        {
            if (dir != Dir.Right && dir != Dir.Up && dir != Dir.Left && dir != Dir.Down)
                return false;

            if (dir == Dir.Right && rx != 0)
                x++;

            if (dir == Dir.Down && ry != 0)
                y++;

            int r;
            if (dir == Dir.Right || dir == Dir.Left)
                r = rx;
            else
                r = ry;

            if ((emeraldField[y * MWIDTH + x] & emmask) != 0)
            {
                if (r == emeraldBox[dir])
                {
                    game.drawing.DrawEmerald(x * 20 + 12, y * 18 + 21);
                    game.IncrementPenalty();
                }
                if (r == emeraldBox[dir + 1])
                {
                    game.drawing.EraseEmerald(x * 20 + 12, y * 18 + 21);
                    game.IncrementPenalty();
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
                game.drawing.EraseEmerald(x * 20 + 12, (y + 1) * 18 + 21);
            }
        }
    }
}
