namespace Compze.Internals.SystemCE;

// ReSharper disable once InconsistentNaming
static class GCCE
{
   public static unit ForceFullGcAllGenerationsAndWaitForFinalizers() => unit.Invoke(() =>
   {
      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
      GC.WaitForFullGCComplete();
      GC.WaitForPendingFinalizers();
   });
}
