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
            loop_error = recfilter_init(tfreq, 0.1);
            PFD_init(ref phase_detector, 0.0);
#if DEBUG
            Log.Write($"inittimer: ftime = {ftime}");
#endif
        }

        public static uint randv;

        public static uint getlrt()
        {
            return 0;
        }

        public static uint gethrt()
        {
            uint add_delay;
            double eval, clk_rl, tfreq, add_delay_d, filterval;
            double cum_error = 0.0;

            if (ftime <= 1)
            {
                doscreenupdate();
                return 0;
            }
            tfreq = 1000000.0 / ftime;
            clk_rl = SDL.GetTicks() * tfreq / 1000.0;
            eval = PFD_get_error(ref phase_detector, clk_rl);
            if (eval != 0)
                filterval = recfilter_apply(ref loop_error, sigmoid(eval));
            else
                filterval = recfilter_getlast(ref loop_error);

            add_delay_d = (freqoff_to_period(tfreq, 1.0, filterval) * 1000.0) + cum_error;
            add_delay = (uint)Math.Round(add_delay_d);
            cum_error = add_delay_d - add_delay;
#if DEBUG
            Log.Write($"clk_rl = {clk_rl:N1}, add_delay = {add_delay}, eval = {eval:N1}, filterval = {filterval:N1}, cum_error = {cum_error:N1}");
#endif

            doscreenupdate();
            SDL.Delay(add_delay);

            return 0;
        }

        public static int getkips()
        {
            return 1;
        }

        public static void s0initint8()
        {
        }

        public static void s0restoreint8()
        {
        }

        public static void s1initint8()
        {
        }

        public static void s1restoreint8()
        {
        }

        public static void s0soundoff()
        {
        }

        public static void s0setspkrt2()
        {
        }

        public static void s0settimer0(ushort t0v)
        {
        }

        public static void s0settimer2(ushort t0v)
        {
        }

        public static void s0timer0(ushort t0v)
        {
        }

        public static void s0timer2(ushort t0v)
        {
        }

        public static void s0soundkillglob()
        {
        }
    }
}