using Xunit.Sdk;
using Xunit.v3;

// Every specification here chooses the process's CompzeLogger.Backend (and some assign Serilog's static Log.Logger or Console.Out),
// so two running at once would write to each other's backend. They take milliseconds in all, so running them one at a time costs nothing.
[assembly: Parallelization(Mode = ParallelMode.None)]
