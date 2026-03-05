using Compze.Internals.SystemCE;
using Xunit;

namespace Compze.Tests.Infrastructure;

/// <summary>
/// Base class for all tests that performs lazy initialization on first test construction.
/// This ensures GC and exception gathering happens only when tests actually run, not during assembly load.
/// This class is internal to avoid ambiguous references with framework-specific UniversalTestBase classes.
/// </summary>
public abstract class UniversalTestBase : IDisposable, IAsyncLifetime
{
   //If we start getting log entries about strictly managed resources not being disposed, setting this to true should let us catch them easily.
   //As long as we have no such issues, leave it at false and the tests run much faster.
   // ReSharper disable once ConvertToConstant.Local
#pragma warning disable IDE0044
   static bool ResourceLeakDebugMode = false;
#pragma warning restore IDE0044

   public void Dispose()
   {
      DisposeInternal();
      if(ResourceLeakDebugMode)
      {
         UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      }
   }

   public async ValueTask InitializeAsync() => await InitializeAsyncInternal();

   public async ValueTask DisposeAsync()
   {
      Dispose();
      await DisposeAsyncInternal();
   }

   protected virtual void DisposeInternal() {}
   protected virtual async Task InitializeAsyncInternal() => await Task.CompletedTask;
   protected virtual async Task DisposeAsyncInternal() => await Task.CompletedTask;
}
