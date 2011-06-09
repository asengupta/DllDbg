using Microsoft.Samples.Debugging.CorDebug;

namespace DllDbg
{
    public interface IMessageSubscriber
    {
        void Published(string message);
        void Stopped(DllDebugger debugger, CorThread thread);
        void PublishedSource(string message);
        void Ask(string question, IInterrogator interrogator);
    }
}