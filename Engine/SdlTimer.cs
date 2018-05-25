using SDL2;
using System;

namespace Digger.Net
{
    public class Timer
    {
        public PFD phase_detector;
        public recfilter loop_error;

        private double cum_error = 0.0;

        public Timer()
        {
            double tfreq = 1000000.0 / DiggerC.g_FrameTime;
            loop_error = Filter.recfilter_init(tfreq, 0.1);
            Filter.PFD_init(ref phase_detector, 0.0);
            DebugLog.Write($"inittimer: ftime = {DiggerC.g_FrameTime}");
        }

        public void SyncFrame()
        {
            if (DiggerC.g_FrameTime <= 1)
                return;

            double tfreq = 1000000.0 / DiggerC.g_FrameTime;
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

            SDL.SDL_Delay(add_delay);
        }
    }
}