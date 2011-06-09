using System;
using System.Diagnostics.SymbolStore;

namespace DllDbg
{
    public class NullSymbolDocument : ISymbolDocument
    {
        public byte[] GetCheckSum()
        {
            throw new NotImplementedException();
        }

        public int FindClosestLine(int line)
        {
            throw new NotImplementedException();
        }

        public byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn)
        {
            throw new NotImplementedException();
        }

        public string URL
        {
            get { return string.Empty; }
        }

        public Guid DocumentType
        {
            get { throw new NotImplementedException(); }
        }

        public Guid Language
        {
            get { throw new NotImplementedException(); }
        }

        public Guid LanguageVendor
        {
            get { throw new NotImplementedException(); }
        }

        public Guid CheckSumAlgorithmId
        {
            get { throw new NotImplementedException(); }
        }

        public bool HasEmbeddedSource
        {
            get { throw new NotImplementedException(); }
        }

        public int SourceLength
        {
            get { throw new NotImplementedException(); }
        }
    }
}