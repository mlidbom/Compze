using System;

namespace Compze.Utilities.SystemCE;

// ReSharper disable once InconsistentNaming
static class GCCE
{
   internal static unit ForceFullGcAllGenerationsAndWaitForFinalizers() => unit.From(() =>
   {
      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
      GC.WaitForFullGCComplete();
      GC.WaitForPendingFinalizers();
   });
}
