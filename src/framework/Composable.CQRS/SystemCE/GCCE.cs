using System;
using Composable.Functional;

namespace Composable.SystemCE;

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
