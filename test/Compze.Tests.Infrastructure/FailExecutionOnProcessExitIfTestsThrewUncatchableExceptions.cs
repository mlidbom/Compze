using System.Runtime.CompilerServices;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE;

namespace Compze.Tests.Infrastructure;

public static class FailExecutionOnProcessExitIfTestsThrewUncatchableExceptions
{
   static ILogger Log => CompzeLogger.For(typeof(FailExecutionOnProcessExitIfTestsThrewUncatchableExceptions));

   [ModuleInitializer]
   public static void Initialize()
   {
      AppDomain.CurrentDomain.ProcessExit += (_, _) =>
      {
         try
         {
            UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
         }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
         catch(Exception ex)
         {
            try
            {
               Log.Error(ex, "UNCATCHABLE EXCEPTIONS DETECTED");
            }
            catch
            {
               // ignore this might be overkill, but with the pretty much undiagnosable NCrunch trouble we keep seeing I figure better safe than sorry.
            }
            Console.Error.WriteLine("""
                                    ========================================
                                         UNCATCHABLE EXCEPTIONS DETECTED
                                    ========================================
                                    """);
            Console.Error.WriteLine(ex);
         }
      };
#pragma warning restore CA1031
   }
}
