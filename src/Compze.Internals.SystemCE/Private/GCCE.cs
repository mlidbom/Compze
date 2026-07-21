using Compze.SystemCE;

namespace Compze.Internals.SystemCE.Private;

// ReSharper disable once InconsistentNaming
static class GCCE
{
   public static Unit ForceFullGcAllGenerationsAndWaitForFinalizers() => Unit.Invoke(() =>
   {
      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
      GC.WaitForFullGCComplete();
      GC.WaitForPendingFinalizers();
   });
}
