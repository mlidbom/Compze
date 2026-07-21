using Compze.SystemCE;

namespace Compze.Internals.SystemCE._private;

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
