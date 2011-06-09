namespace DllDbg
{
    public class UntrackedSequencePoint : SequencePoint
    {
        public UntrackedSequencePoint(uint offset) : base((int) offset, new NullSymbolDocument(), 0, 0, 0, 0)
        {
        }
    }
}