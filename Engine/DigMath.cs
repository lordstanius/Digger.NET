/*
 * Copyright (c) 2014 Sippy Software, Inc., http://www.sippysoft.com
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */

using System;

namespace Digger.Net
{
    public struct recfilter
    {
        public double a;
        public double b;
        public double lastval;
        public double minval;
        public double maxval;
        public int peak_detect;
    }

    public struct PFD
    {
        public double target_clk;
        public double phi_round;
    };

    public struct bqd_filter
    {
        public double a1;
        public double b0;
        public double b1;
        public double z0;
        public double z1;
    };

    public static class Math
    {
        public static void PFD_init(ref PFD pfd_p, double phi_round)
        {
            pfd_p.target_clk = 0.0;
            pfd_p.phi_round = phi_round;
        }

        public static double PFD_get_error(ref PFD pfd_p, double dtime)
        {
            double next_clk, err0r;

            if (pfd_p.phi_round > 0.0)
                dtime = System.Math.Truncate(dtime * pfd_p.phi_round) / pfd_p.phi_round;

            next_clk = System.Math.Truncate(dtime) + 1.0;
            if (pfd_p.target_clk == 0.0)
            {
                pfd_p.target_clk = next_clk;
                return (0.0);
            }

            err0r = pfd_p.target_clk - dtime;

            if (err0r > 0)
                pfd_p.target_clk = next_clk + 1.0;
            else
                pfd_p.target_clk = next_clk;

            return (err0r);
        }

        public static double sigmoid(double x)
        {
            return x / (1 + System.Math.Abs(x));
        }

        public static void _recfilter_peak_detect(ref recfilter f)
        {
            if (f.lastval > f.maxval)
                f.maxval = f.lastval;

            if (f.lastval < f.minval)
                f.minval = f.maxval;
        }

        public static double recfilter_apply(ref recfilter f, double x)
        {
            f.lastval = f.a * x + f.b * f.lastval;
            if (f.peak_detect != 0)
                _recfilter_peak_detect(ref f);

            return f.lastval;
        }

        public static double recfilter_apply_int(ref recfilter f, int x)
        {
            f.lastval = f.a * (double)(x) + f.b * f.lastval;
            if (f.peak_detect != 0)
                _recfilter_peak_detect(ref f);

            return f.lastval;
        }

        public static recfilter recfilter_init(double Fs, double Fc)
        {
            recfilter f = new recfilter();

            if (Fs < Fc * 2.0)
            {
                DebugLog.Write($"recfilter_init: cutoff frequency ({Fc:N1}) should be less than half of the sampling rate ({Fs:N1})");
                Environment.Exit(0);
            }

            f.b = System.Math.Exp(-2.0 * System.Math.PI * Fc / Fs);
            f.a = 1.0 - f.b;
            return f;
        }

        public static double recfilter_getlast(ref recfilter f)
        {
            return (f.lastval);
        }

        public static void recfilter_setlast(ref recfilter f, double val)
        {
            f.lastval = val;
            if (f.peak_detect != 0)
                _recfilter_peak_detect(ref f);
        }

        public static void recfilter_peak_detect(ref recfilter f)
        {
            f.peak_detect = 1;
            f.maxval = f.lastval;
            f.minval = f.lastval;
        }

        public static double freqoff_to_period(double freq_0, double foff_c, double foff_x)
        {
            return (1.0 / freq_0 * (1 + foff_c * foff_x));
        }

        public static bqd_filter bqd_lp_init(double Fs, double Fc)
        {
            bqd_filter fp = new bqd_filter();
            double n, w;

            if (Fs < Fc * 2.0)
            {
                DebugLog.Write($"fo_init: cutoff frequency ({Fc:N1}) should be less than half of the sampling rate ({Fs:N2})");
                Environment.Exit(0);
            }
            w = System.Math.Tan(System.Math.PI * Fc / Fs);
            n = 1.0 / (1.0 + w);
            fp.a1 = n * (w - 1);
            fp.b0 = n * w;
            fp.b1 = fp.b0;
            return (fp);
        }

        public static bqd_filter bqd_hp_init(double Fs, double Fc)
        {
            bqd_filter fp = new bqd_filter();
            double n, w;

            if (Fs < Fc * 2.0)
            {
                DebugLog.Write($"fo_init: cutoff frequency ({Fc:N1}) should be less than half of the sampling rate ({Fs:N2})");
                Environment.Exit(0);
            }

            w = System.Math.Tan(System.Math.PI * Fc / Fs);
            n = 1.0 / (1.0 + w);
            fp.a1 = n * (w - 1);
            fp.b0 = n;
            fp.b1 = -fp.b0;
            return (fp);
        }

        public static double bqd_apply(ref bqd_filter fp, double x)
        {
            fp.z1 = (x * fp.b0) + (fp.z0 * fp.b1) - (fp.z1 * fp.a1);
            fp.z0 = x;
            return (fp.z1);
        }
    }
}