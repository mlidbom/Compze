using System;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Base class for all tests that performs lazy initialization on first test construction.
/// This ensures GC and exception gathering happens only when tests actually run, not during assembly load.
/// This class is internal to avoid ambiguous references with framework-specific UniversalTestBase classes.
/// </summary>
public abstract class UniversalTestBase : IDisposable
{
   bool _disposed;

   public void Dispose()
   {
      Dispose(true);
      GC.SuppressFinalize(this);
   }

   protected virtual void Dispose(bool disposing)
   {
      if(_disposed) return;

      if(disposing)
         SurfaceAnyUncatchableExceptions();

      _disposed = true;
   }

   static void SurfaceAnyUncatchableExceptions() => UncatchableExceptionsGatherer.ConsumeAndThrowAnyExceptionsGathered();
}
