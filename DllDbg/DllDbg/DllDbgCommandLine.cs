using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DllDbg
{
    public class DllDbgCommandLine
    {
        public static void Main(string[] args)
        {
            var debugger = new DllDebugger(new TextMessageSubscriber(), new GitSourceRepository(new UserInputParser(), new DirectoryInfo("f:\\checked_out")));
            var process = Process.GetProcessesByName("SampleCecilTestbed").First();
            debugger.Debug(process.Id);
            while (debugger.NumberOfModulesLoaded < 2)
            {
                
            }
            Console.Out.WriteLine();
            debugger.SetBreakPoint(47, "SampleCecil", "Class1");
//            debugger.SetBreakPoint(56, "SampleCecil", "Class1");
            debugger.SetBreakPoint(61, "SampleCecil", "Class1");
            debugger.WaitTillExit();
        }
    }
}