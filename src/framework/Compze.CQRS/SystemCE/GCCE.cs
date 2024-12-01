using System;
using Compze.Functional;

namespace Compze.SystemCE;

// ReSharper disable once InconsistentNaming
static class GCCE
{
   internal static Unit ForceFullGcAllGenerationsAndWaitForFinalizers() => Unit.From(() =>
   {
      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
      GC.WaitForFullGCComplete();
      GC.WaitForPendingFinalizers();
   });
}
