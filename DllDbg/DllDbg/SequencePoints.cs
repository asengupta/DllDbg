using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using Microsoft.Samples.Debugging.CorDebug;

namespace DllDbg
{
    public class SequencePoints : List<SequencePoint>
    {
        private SequencePoints(int[] offsets, ISymbolDocument[] documents, int[] lines, int[] columns, int[] endLines,
                              int[] endColumns, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (offsets[i] == 0xfeefee) continue;
                Add(new SequencePoint(offsets[i], documents[i], lines[i], columns[i], endLines[i], endColumns[i]));
            }
        }

        public static SequencePoints Empty
        {
            get { return new SequencePoints(new int[0], new ISymbolDocument[0], new int[0], new int[0], new int[0], new int[0], 0); }
        }

        public static SequencePoints From(ISymbolMethod method)
        {
            if (method == null) throw new NoDebugInfo();
            int count = method.SequencePointCount;
            var offsets = new int[count];
            var symbolDocuments = new ISymbolDocument[count];
            var startLines = new int[count];
            var startColumns = new int[count];
            var endLines = new int[count];
            var endColumns = new int[count];
            method.GetSequencePoints(offsets, symbolDocuments, startLines, startColumns, endLines, endColumns);
            return new SequencePoints(offsets, symbolDocuments, startLines, startColumns, endLines, endColumns, count);
        }

        public SequencePoint PointAt(uint offset)
        {
            var find = Find(point => point.Offset == offset);
            if (find == null) return new UntrackedSequencePoint(offset);
            return find;
        }

        public SequencePoint PointAfter(SequencePoint point, CorThread thread)
        {
            try
            {
                return point is UntrackedSequencePoint ? BestGuess(point) : this[IndexOf(point) + 1];
            }
            catch (ArgumentOutOfRangeException e)
            {
                return new UntrackedSequencePoint((uint) thread.ActiveFrame.Function.ILCode.Size);
            }
            catch (InvalidOperationException e)
            {
                return new UntrackedSequencePoint((uint) thread.ActiveFrame.Function.ILCode.Size);
            }
        }

        private SequencePoint BestGuess(SequencePoint point)
        {
            foreach (var sequencePoint in this)
            {
                if (sequencePoint.Offset > point.Offset) return sequencePoint;
            }
            return this.Last();
        }
    }

    public class NoDebugInfo : Exception
    {
    }
}