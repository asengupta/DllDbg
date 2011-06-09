using System.Diagnostics.SymbolStore;

namespace DllDbg
{
    public class SequencePoint
    {
        private readonly ISymbolDocument document;
        private readonly int endColumn;
        private readonly int endLine;
        private readonly int offset;
        private readonly int startColumn;
        private readonly int startLine;

        public SequencePoint(int offset, ISymbolDocument document, int startLine, int startColumn, int endLine,
                             int endColumn)
        {
            this.offset = offset;
            this.document = document;
            this.startLine = startLine;
            this.startColumn = startColumn;
            this.endLine = endLine;
            this.endColumn = endColumn;
        }

        public int Offset
        {
            get { return offset; }
        }

        public ISymbolDocument Document
        {
            get { return document; }
        }

        public int StartLine
        {
            get { return startLine; }
        }

        public int StartColumn
        {
            get { return startColumn; }
        }

        public int EndLine
        {
            get { return endLine; }
        }

        public int EndColumn
        {
            get { return endColumn; }
        }
    }
}