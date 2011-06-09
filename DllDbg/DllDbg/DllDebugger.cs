using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata;
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;
using Microsoft.Samples.Debugging.CorSymbolStore;

namespace DllDbg
{
    public class DllDebugger
    {
        private readonly List<CorModule> modules = new List<CorModule>();

        private readonly IDictionary<CorModule, ISymbolReader> readersByModule =
            new Dictionary<CorModule, ISymbolReader>();

        private readonly ISourceRepository sourceRepository;
        private readonly IMessageSubscriber subscriber;
        private volatile ProcessState state;

        private CorStepper stepper;
        private CorProcess process;
        private CorDebugger debugger;
        private readonly List<CorFunctionBreakpoint> breakpoints = new List<CorFunctionBreakpoint>();

        public DllDebugger(IMessageSubscriber subscriber, ISourceRepository sourceRepository)
        {
            this.subscriber = subscriber;
            this.sourceRepository = sourceRepository;
            NumberOfModulesLoaded = 0;
        }

        public int NumberOfModulesLoaded { get; private set; }

        private void OnExit(object sender, CorProcessEventArgs e)
        {
            state = ProcessState.Terminated;
            subscriber.Published(string.Format("Process [{0}] terminated, exiting...", e.Process.Id));
        }

        private void OnNewAppDomain(object sender, CorAppDomainEventArgs e)
        {
            subscriber.Published(string.Format("Attaching to appDomain [{0}]...", e));
            e.AppDomain.Attach();
        }

        private void OnException(object sender, CorExceptionEventArgs e)
        {
            Console.Out.WriteLine("Exception {0}...", e);
        }

        public void Debug(int processID)
        {
            debugger = new CorDebugger(CorDebugger.GetDefaultDebuggerVersion());
            process = debugger.DebugActiveProcess(processID, false);
            process.OnException += OnException;
            process.OnCreateAppDomain += OnNewAppDomain;
            process.OnProcessExit += OnExit;
            process.OnModuleLoad += OnModuleLoad;
            process.OnBreakpoint += OnBreakpoint;
            process.OnStepComplete += OnStepComplete;
            process.Continue(false);
            state = ProcessState.Started;
            subscriber.Published(string.Format("Successfully attached to Process with ID [{0}]", processID));
        }

        private void OnStepComplete(object sender, CorStepCompleteEventArgs e)
        {
            try
            {
                Paused(e.Thread);
            }
            catch (NoDebugInfo i)
            {
                subscriber.Published("There is no debug info for this point. Stepping out of debug lock to continue...");
                stepper = Stepper(e.Thread);
                stepper.StepOut();
            }
        }

        private CorStepper Stepper(CorThread thread)
        {
            return thread.ActiveFrame.CreateStepper();
        }

        private void OnBreakpoint(object sender, CorBreakpointEventArgs e)
        {
            subscriber.Published("Hit Breakpoint.");
            Paused(e.Thread);
        }

        private void Paused(CorThread thread)
        {
            stepper = Stepper(thread);
            SequencePoints points = SequencePointsFor(thread);
            uint currentOffset = CurrentOffset(thread);
            SequencePoint sequencePoint = points.PointAt(currentOffset);
            string source = new FileReader().Read(sequencePoint.Document.URL, sequencePoint.StartLine,
                                                  sequencePoint.StartColumn, sequencePoint.EndLine,
                                                  sequencePoint.EndColumn);
            subscriber.Published("Stopped at:");
            subscriber.PublishedSource(source);
            subscriber.Stopped(this, thread);
        }

        public void StepOver(CorThread t)
        {
            Step(t, false);
        }

        public void StepInto(CorThread t)
        {
            Step(t, true);
        }

        private void Step(CorThread t, bool stepInto)
        {
            uint offset = CurrentOffset(t);
            SequencePoints points = SequencePointsFor(t);
            SequencePoint sequencePoint = points.PointAt(CurrentOffset(t));
            SequencePoint nextPoint = points.PointAfter(sequencePoint, t);
            stepper.SetUnmappedStopMask(CorDebugUnmappedStop.STOP_NONE);
            stepper.StepRange(stepInto,
                              new[]
                                  {new COR_DEBUG_STEP_RANGE {startOffset = offset, endOffset = (uint) nextPoint.Offset}});
        }

        private uint CurrentOffset(CorThread t)
        {
            uint offset;
            CorDebugMappingResult mappingResult;
            t.ActiveFrame.GetIP(out offset, out mappingResult);
            return offset;
        }

        private SequencePoints SequencePointsFor(CorThread thread)
        {
            if (!readersByModule.ContainsKey(thread.ActiveFrame.Function.Module)) return SequencePoints.Empty;
            ISymbolReader reader = readersByModule[thread.ActiveFrame.Function.Module];
            ISymbolMethod method = reader.GetMethod(new SymbolToken(thread.ActiveFrame.FunctionToken));
            return SequencePoints.From(method);
        }

        public void SetBreakPoint(int lineNumber, string moduleNameHint, string fileNameHint)
        {
            if (state == ProcessState.Terminated) throw new InvalidProcessException();
            List<CorModule> matchingModules =
                modules.Where(module => module.Name.Contains(moduleNameHint)).ToList();
            if (matchingModules.Count > 1) throw new MultipleMatchException();
            if (matchingModules.Count == 0) throw new NoMatchException();
            CorModule corModule = matchingModules.First();
            var pdbFile = new FileInfo(corModule.Name);
            var binder = new SymbolBinder();
            var metadataImport = corModule.GetMetaDataInterface<IMetadataImport>();
            ISymbolReader reader = binder.GetReaderForFile(metadataImport, pdbFile.FullName, null,
                                                           SymSearchPolicies.AllowOriginalPathAccess |
                                                           SymSearchPolicies.AllowReferencePathAccess);

            Index(reader, corModule);
            ISymbolDocument[] documents = reader.GetDocuments();
            List<ISymbolDocument> matchingDocuments =
                documents.Where(document => Regex.Match(document.URL, fileNameHint).Success).ToList();
            if (matchingDocuments.Count > 1) throw new MultipleMatchException();
            if (matchingDocuments.Count == 0) throw new NoMatchException();
            ISymbolDocument source = matchingDocuments.First();
            int line = source.FindClosestLine(lineNumber);
            ISymbolMethod method = reader.GetMethodFromDocumentPosition(source, line, 1);
            CorFunction function = corModule.GetFunctionFromToken(method.Token.GetToken());
            SequencePoints points = SequencePoints.From(method);
            SequencePoint pointToBreak =
                points.Where(point => lineNumber >= point.StartLine && line <= point.EndLine).First();

            CorFunctionBreakpoint breakpoint = function.ILCode.CreateBreakpoint(pointToBreak.Offset);
            breakpoints.Add(breakpoint);
            breakpoint.Activate(true);
            subscriber.Published(string.Format("Successfully set breakpoint on line {0} in file {1} in module {2}",
                                               lineNumber, source.URL, corModule.Name));
        }

        private void Index(ISymbolReader reader, CorModule module)
        {
            if (readersByModule.ContainsKey(module)) readersByModule[module] = reader;
            else readersByModule.Add(module, reader);
        }

        private void OnModuleLoad(object sender, CorModuleEventArgs e)
        {
            subscriber.Published(string.Format("Loading module: {0}", e.Module.Name));
            modules.Add(e.Module);
            Assembly assembly = Assembly.LoadFile(e.Module.Name);
            object[] attributes = assembly.GetCustomAttributes(true);
            try
            {
                var revisionAttribute =
                    (AssemblyInformationalVersionAttribute)
                    attributes.First(o => o is AssemblyInformationalVersionAttribute);
                string revision = revisionAttribute.InformationalVersion;
                sourceRepository.Verify(revision, subscriber);
            }
            catch (InvalidOperationException ioex)
            {
                subscriber.Published(
                    string.Format(
                        "No source revision found for module {0}, will not use this module for tracking source.",
                        e.Module.Name));
            }

            subscriber.Published(string.Format("Module loaded: {0}", e.Module.Name));
            NumberOfModulesLoaded++;
        }

        public void WaitTillExit()
        {
            while (state == ProcessState.Started) ;
        }

        public void ShowStackTrace(CorThread thread)
        {
            var builder = new StringBuilder();
            foreach (CorFrame frame in thread.ActiveChain.Frames)
            {
                if (frame.FrameType == CorFrameType.ILFrame) builder.AppendLine("[IL]");
                var import = new CorMetadataImport(frame.Function.Module);
                MethodInfo methodInfo = import.GetMethodInfo(frame.FunctionToken);
                builder.AppendLine(string.Format("{0}:{1}", methodInfo.DeclaringType, methodInfo.Name));
            }
            subscriber.PublishedSource(builder.ToString());
            subscriber.Stopped(this, thread);
        }

        public void ShowLocals(CorThread thread)
        {
            subscriber.Published(string.Format("Number of local variables={0}",
                                               thread.ActiveFrame.GetLocalVariablesCount()));
            var builder = new StringBuilder();
            for (int i = 0; i < thread.ActiveFrame.GetLocalVariablesCount(); i++)
            {
                CorValue variable = thread.ActiveFrame.GetLocalVariable(i);

                builder.AppendLine(string.Format("local_{0}={1}", i, variable.CastToObjectValue().Type));
            }
            subscriber.Published(builder.ToString());
            subscriber.Stopped(this, thread);
        }

        public void Detach()
        {
            subscriber.Published("Detaching from process [" + process.Id + "]...");
            try
            {
                breakpoints.ForEach(breakpoint =>
                                        {
                                            breakpoint.Activate(false);
                                            subscriber.Published(string.Format("Deactivating breakpoint at offset {0} in function [{1}]", breakpoint.Offset, breakpoint.Function.Token));
                                        });
                process.OnException -= OnException;
                process.OnCreateAppDomain -= OnNewAppDomain;
                process.OnProcessExit -= OnExit;
                process.OnModuleLoad -= OnModuleLoad;
                process.OnBreakpoint -= OnBreakpoint;
                process.OnStepComplete -= OnStepComplete;
                debugger.GetProcess(process.Id).Detach();
                subscriber.Published("Successfully detached from process [" + process.Id + "]...");
                state = ProcessState.Terminated;
            }
            catch (Exception e)
            {
                subscriber.Published("Failed to detach from process [" + process.Id + "]. The debugger is in an inconsistent state :-( Exception is: " + e);
            }
        }
    }
}