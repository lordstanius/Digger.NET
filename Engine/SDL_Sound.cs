using SDL2;
using System;
using System.Runtime.InteropServices;

namespace Digger.Net
{
    public class SDL_Sound
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct sudata
        {
            public SDL.SDL_AudioSpec obtained;
            public IntPtr buf;
            public uint bsize;
            public IntPtr lp_fltr;
            public IntPtr hp_fltr;
        };

        private static SDL.SDL_AudioCallback pFillAudio = FillAudio;
        private static Func<byte> GetSample;

        public bool IsWaveDeviceAvailable { get; private set; }

        public bool SetDevice(ushort sampleRate, ushort bufferSize, Func<byte> getSample)
        {
            GetSample = getSample;
            SDL.SDL_AudioSpec wanted = new SDL.SDL_AudioSpec();

            sudata sud = new sudata();
            bool result = false;

            wanted.freq = sampleRate;
            wanted.samples = bufferSize;
            wanted.channels = 1;
            wanted.format = SDL.AUDIO_S16;
            wanted.userdata = Marshal.AllocHGlobal(Marshal.SizeOf(sud));
            wanted.callback = pFillAudio;

            if ((SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO)) == 0)
                if ((SDL.SDL_OpenAudio(ref wanted, out sud.obtained)) == 0)
                    result = true;

            if (result == false)
            {
                Log.Write($"Couldn't open audio: {SDL.SDL_GetError()}");
                Marshal.FreeHGlobal(wanted.userdata);
                return false;
            }

            sud.bsize = sud.obtained.size;
            sud.buf = Marshal.AllocHGlobal((int)sud.bsize);

            sud.lp_fltr = Math.bqd_lp_init(sud.obtained.freq, 4000).ToPointer();
            sud.hp_fltr = Math.bqd_hp_init(sud.obtained.freq, 1000).ToPointer();

            Marshal.StructureToPtr(sud, wanted.userdata, false);
            IsWaveDeviceAvailable = true;

            return result;
        }

        private static void FillAudio(IntPtr udata, IntPtr stream, int len)
        {
            sudata sud = udata.ToStruct<sudata>();
            if (len > sud.bsize)
            {
                Log.Write("fill_audio: OUCH, len > bsize!");
                len = (int)sud.bsize;
            }

            StdLib.MemSet(stream, sud.obtained.silence, len);
            for (int i = 0; i < len / sizeof(short); i++)
            {
                bqd_filter hp_fltr = sud.hp_fltr.ToStruct<bqd_filter>();
                bqd_filter lp_fltr = sud.lp_fltr.ToStruct<bqd_filter>();

                double sample = GetSample();
                sample = Math.bqd_apply(ref hp_fltr, (sample - 127.0) * 128.0);
                sample = Math.bqd_apply(ref lp_fltr, sample);
                Marshal.WriteInt16(sud.buf, i * sizeof(short), (short)System.Math.Round(sample));
            }

            SDL.SDL_MixAudioFormat(stream, sud.buf, sud.obtained.format, (uint)len, SDL.SDL_MIX_MAXVOLUME);
        }

        public void DisableSound()
        {
            SDL.SDL_PauseAudio(1);
        }

        public void EnableSound()
        {
            SDL.SDL_PauseAudio(0);
        }
    }
}