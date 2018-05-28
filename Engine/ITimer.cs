namespace Digger.Net
{
    public interface ITimer
    {
        void SyncFrame();
        uint FrameTime { get; set; }
    }
}
