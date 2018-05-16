using SDL2;
using System;
using System.Runtime.InteropServices;

namespace Digger.Net
{
    public static partial class DiggerC
    {
        public static bool wave_device_available = false;

        public static bool initsounddevice()
        {
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct sudata
        {
            public SDL.AudioSpec obtained;
            public IntPtr buf;
            public uint bsize;
            public IntPtr lp_fltr;
            public IntPtr hp_fltr;
        };

        public static bool setsounddevice(ushort samprate, ushort bufsize)
        {
            return false;
            SDL.AudioSpec wanted = new SDL.AudioSpec();

            sudata sud = new sudata();
            IntPtr pSud = Marshal.AllocHGlobal(Marshal.SizeOf(sud));
            Marshal.StructureToPtr(sud, pSud, false);
            bool result = false;

            wanted.freq = samprate;
            wanted.samples = bufsize;
            wanted.channels = 1;
            wanted.format = SDL.AUDIO_S16;
            wanted.userdata = pSud;
            //wanted.callback = fill_audio;

            if ((SDL.Init(SDL.INIT_AUDIO)) >= 0)
                if ((SDL.OpenAudio(ref wanted, out sud.obtained)) >= 0)
                    result = true;

            if (result == false)
            {
                Log.Write($"Couldn't open audio: %{SDL.GetError()}");
                Marshal.FreeHGlobal(pSud);
                return (false);
            }

            sud.bsize = sud.obtained.size;
            //sud.buf = new short[sud.bsize];

            Marshal.StructureToPtr(bqd_lp_init(sud.obtained.freq, 4000), sud.lp_fltr, false);
            Marshal.StructureToPtr(bqd_hp_init(sud.obtained.freq, 1000), sud.hp_fltr, false);
            wave_device_available = true;

            return (result);
        }

        public static void fill_audio(IntPtr udata, IntPtr stream, int len)
        {
            sudata sud = (sudata)Marshal.PtrToStructure(udata, typeof(sudata));
            if (len > sud.bsize)
            {
                Log.Write("fill_audio: OUCH, len > bsize!");
                len = (int)sud.bsize;
            }
            byte[] bstream = new byte[len];
            StdLib.MemSet(bstream, sud.obtained.silence, len);
            for (int i = 0; i < len / sizeof(short); i++)
            {
                byte sample = getsample();
                //double dsample = (byte)bqd_apply(ref sud.hp_fltr, (sample - 127.0) * 128.0);
                //IntPtr.Add(sud.buf, i) = (short)Math.Round(bqd_apply(ref sud.lp_fltr, dsample));
            }

            var soundBuffer = new byte[len];
            //Buffer.BlockCopy(sud.buf, 0, soundBuffer, 0, len);
            SDL.MixAudioFormat(bstream, soundBuffer, sud.obtained.format, (uint)len, SDL.MIX_MAXVOLUME);
            Marshal.Copy(bstream, 0, stream, bstream.Length);
        }

        public static void killsounddevice()
        {
            SDL.PauseAudio(1);
        }
    }
}