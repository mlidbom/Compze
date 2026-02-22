using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE;

// ReSharper disable once InconsistentNaming
internal static class GCCE
{
   public static unit ForceFullGcAllGenerationsAndWaitForFinalizers() => unit.From(() =>
   {
      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
      GC.WaitForFullGCComplete();
      GC.WaitForPendingFinalizers();
   });
}
