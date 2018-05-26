using SDL2;

namespace Digger.Net
{
    public class SdlTimer
    {
        public PFD phase_detector;
        public recfilter loop_error;

        private double cum_error = 0.0;
        public uint FrameTime;

        public SdlTimer()
        {
            double tfreq = 1000000.0 / FrameTime;
            loop_error = Math.recfilter_init(tfreq, 0.1);
            Math.PFD_init(ref phase_detector, 0.0);
            DebugLog.Write($"inittimer: ftime = {FrameTime}");
        }

        public void SyncFrame()
        {
            if (FrameTime <= 1)
                return;

            double tfreq = 1000000.0 / FrameTime;
            double clk_rl = SDL.SDL_GetTicks() * tfreq / 1000.0;
            double eval = Math.PFD_get_error(ref phase_detector, clk_rl);
            double filterval;
            if (eval != 0)
                filterval = Math.recfilter_apply(ref loop_error, Math.sigmoid(eval));
            else
                filterval = Math.recfilter_getlast(ref loop_error);

            double add_delay_d = (Math.freqoff_to_period(tfreq, 1.0, filterval) * 1000.0) + cum_error;
            uint add_delay = (uint)System.Math.Round(add_delay_d);
            cum_error = add_delay_d - add_delay;
            DebugLog.Write($"clk_rl = {clk_rl}, add_delay = {add_delay}, eval = {eval}, filterval = {filterval}, cum_error = {cum_error}");

            SDL.SDL_Delay(add_delay);
        }
    }
}