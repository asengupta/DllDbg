namespace DllDbg
{
    public interface ISourceRepository
    {
        void Verify(string revision, IMessageSubscriber subscriber);
    }
}