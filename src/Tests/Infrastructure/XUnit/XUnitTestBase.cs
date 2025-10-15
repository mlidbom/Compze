using System;
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.XUnit;

public class XUnitTestBase : UniversalTestBase, IDisposable
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
