using System;
using System.Runtime.CompilerServices;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;

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
            Console.Error.WriteLine($"""
                                     ========================================
                                          UNCATCHABLE EXCEPTIONS DETECTED
                                     ========================================
                                     """);
            Console.Error.WriteLine(ex);
         }
      };
   }
}
