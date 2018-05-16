using SDL2;

namespace Digger.Net
{
    public sealed class digger_draw_api
    {
        public delegate void ginit();
        public delegate void gclear();
        public delegate void gpal(int pal);
        public delegate void ginten(int inten);
        public delegate void gputi(int x, int y, Surface surface, int w, int h);
        public delegate void ggeti(int x, int y, ref Surface tmp, int w, int h);
        public delegate void gputim(int x, int y, int ch, int w, int h);
        public delegate short ggetpix(int x, int y);
        public delegate void gtitle();
        public delegate void gwrite(int x, int y, int ch, int c);
        public delegate void gflush();

        public ginit init;
        public gclear clear;
        public gpal pal;
        public ginten inten;
        public gputi puti;
        public ggeti geti;
        public gputim putim;
        public ggetpix getpix;
        public gtitle title;
        public gwrite write;
        public gflush flush;
    }
}