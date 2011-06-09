using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DllDbg
{
    public class FileReader
    {
        public string Read(string fileName, int startLine, int startColumn, int endLine, int endColumn)
        {
            try
            {
                var lines = new List<string>();
                using (var r = new StreamReader(fileName))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                var builder = new StringBuilder();
                if (startLine == endLine)
                {
                    lines[startLine - 1] = lines[startLine - 1].Substring(startColumn - 1, endColumn - startColumn);
                }
                else
                {
                    lines[startLine - 1] = lines[startLine - 1].Substring(startColumn - 1);
                    lines[endLine - 1] = lines[endLine - 1].Substring(0, endColumn - 1);
                }
                for (int i = startLine - 1; i <= endLine - 1; i++)
                {
                    builder.AppendLine(lines[i]);
                }

                return builder.ToString();
            }
            catch (Exception e)
            {
                return "[NULL]";
            }
        }

    }
}