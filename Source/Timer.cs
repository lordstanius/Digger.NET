using SDL2;

namespace Digger.Source
{
    public class SDL_Timer
    {
        public PFD phase_detector;
        public recfilter loop_error;

        private double cum_error = 0.0;

        public SDL_Timer()
        {
            double tfreq = 1000000.0 / FrameTicks;
            loop_error = Calc.recfilter_init(tfreq, 0.1);
            Calc.PFD_init(ref phase_detector, 0.0);
        }

        public uint FrameTicks { get; set; }

        public void SyncFrame()
        {
            if (FrameTicks <= 1)
                return;

            double fps = 1000000.0 / FrameTicks;
            double clk_rl = SDL.SDL_GetTicks() * fps / 1000.0;
            double eval = Calc.PFD_get_error(ref phase_detector, clk_rl);
            double filterval = eval != 0 ?
                Calc.recfilter_apply(ref loop_error, Calc.sigmoid(eval)):
                Calc.recfilter_getlast(ref loop_error);

            double add_delay_d = (Calc.freqoff_to_period(fps, 1.0, filterval) * 1000.0) + cum_error;
            uint add_delay = (uint)System.Math.Round(add_delay_d);
            cum_error = add_delay_d - add_delay;
            SDL.SDL_Delay(add_delay);
        }
    }
}