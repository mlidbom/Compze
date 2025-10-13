using System.Threading;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Base class for all tests that performs lazy initialization on first test construction.
/// This ensures GC and exception gathering happens only when tests actually run, not during assembly load.
/// This class is internal to avoid ambiguous references with framework-specific UniversalTestBase classes.
/// </summary>
public abstract class UniversalTestBase
{
   static int _hasPerformedLazySetup;

   protected UniversalTestBase()
   {
      // Lazy initialization - runs exactly once on first test construction
      if(Interlocked.CompareExchange(ref _hasPerformedLazySetup, 1, 0) == 0)
      {
         PerformLazySetup();
      }
   }

   static void PerformLazySetup()
   {
      // Force GC and surface any uncatchable exceptions from previous test runs
      // This is done lazily to avoid breaking NCrunch during assembly load
      UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
   }
}
