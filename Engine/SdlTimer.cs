using SDL2;
using System;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public static PFD phase_detector;
        public static recfilter loop_error;

        public static void inittimer()
        {
            double tfreq;

            tfreq = 1000000.0 / ftime;
            loop_error = Filter.recfilter_init(tfreq, 0.1);
            Filter.PFD_init(ref phase_detector, 0.0);
            DebugLog.Write($"inittimer: ftime = {ftime}");
        }

        public static uint randv;

        public static uint getlrt()
        {
            return 0;
        }

        private static double cum_error = 0.0;

        public static uint gethrt()
        {
            if (ftime <= 1)
            {
                sdlGfx.UpdateScreen();
                return 0;
            }

            double tfreq = 1000000.0 / ftime;
            double clk_rl = SDL.SDL_GetTicks() * tfreq / 1000.0;
            double eval = Filter.PFD_get_error(ref phase_detector, clk_rl);
            double filterval;
            if (eval != 0)
                filterval = Filter.recfilter_apply(ref loop_error, Filter.sigmoid(eval));
            else
                filterval = Filter.recfilter_getlast(ref loop_error);

            double add_delay_d = (Filter.freqoff_to_period(tfreq, 1.0, filterval) * 1000.0) + cum_error;
            uint add_delay = (uint)Math.Round(add_delay_d);
            cum_error = add_delay_d - add_delay;
            DebugLog.Write($"clk_rl = {clk_rl}, add_delay = {add_delay}, eval = {eval}, filterval = {filterval}, cum_error = {cum_error}");
            sdlGfx.UpdateScreen();
            SDL.SDL_Delay(add_delay);

            return 0;
        }

        public static int getkips()
        {
            return 1;
        }
    }
}