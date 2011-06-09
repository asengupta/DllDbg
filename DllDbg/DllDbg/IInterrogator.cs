namespace DllDbg
{
    public interface IInterrogator
    {
        void AnsweredWith(string response, IMessageSubscriber subscriber);
    }
}