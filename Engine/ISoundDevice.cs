namespace Digger.Net
{
    public delegate byte GetSampleDelegate();

    public interface ISoundDevice
    {
        bool IsWaveDeviceAvailable { get; }

        bool SetDevice(ushort sampleRate, ushort bufferSize, GetSampleDelegate getSample);
        void DisableSound();
        void EnableSound();
    }
}
