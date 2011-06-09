using System;
using DllDbg;
using Microsoft.Samples.Debugging.CorDebug;

namespace DllDbg
{
    public class TextMessageSubscriber : IMessageSubscriber
    {
        public void Published(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Out.WriteLine(message);
        }

        public void Stopped(DllDebugger debugger, CorThread thread)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.Out.Write("Thread [{0}]", thread.Id);
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Out.Write(" (ct=Continue, stov=Step Over, stin=Step In, trc=Stack Trace, det=Detach): ");
            Console.ForegroundColor = ConsoleColor.Green;
            var line = Console.In.ReadLine();
            var transcribed = line.ToLower();
            if (transcribed.Contains("ct")) return;
            if (transcribed.Contains("stov")) debugger.StepOver(thread);
            else if (transcribed.Contains("stin")) debugger.StepInto(thread);
            else if (transcribed.Contains("trc")) debugger.ShowStackTrace(thread);
            else if (transcribed.Contains("locals")) debugger.ShowLocals(thread);
            else if (transcribed.Contains("br")) SetBreakpoint(transcribed, debugger);
            else if (transcribed.Contains("det")) debugger.Detach();
            else Stopped(debugger, thread);
        }

        private void SetBreakpoint(string transcribed, DllDebugger debugger)
        {
            var strings = transcribed.Split(' ');
            try
            {
                debugger.SetBreakPoint(int.Parse(strings[1]), strings[2], strings[3]);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Could not set breakpoint.");
            }
        }

        public void PublishedSource(string message)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.Out.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public void Ask(string question, IInterrogator interrogator)
        {
            Console.Out.Write(question);
            interrogator.AnsweredWith(Console.In.ReadLine(), this);
        }
    }
}