using System.Diagnostics;
using System.IO;
using DllDbg;
using NUnit.Framework;
using System.Linq;

namespace DllDbg
{
    [TestFixture]
    public class DllDebuggerTest
    {
        [Test]
        public void CanAttach()
        {
            var debugger = new DllDebugger(new TextMessageSubscriber(), new GitSourceRepository(new UserInputParser(), new DirectoryInfo("f:\\checked_out")));
            var process = Process.GetProcessesByName("SampleCecilTestbed").First();
            debugger.Debug(process.Id);
            debugger.SetBreakPoint(56, "SampleCecil", "Class1");
            debugger.WaitTillExit();
        }
    }
}