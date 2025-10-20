using System;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.NCrutchErrorReproductions;

//brought in to stay similar to the original error
public abstract class UniversalTestBaseNCrunch : IDisposable, IAsyncLifetime
{
   //If we start getting log entries about strictly managed resources not being disposed, setting this to true should let us catch them easily.
   //As long as we have no such issues, leave it at false and the tests run much faster.
   const bool ResourceLeakDebugMode = false;

   public void Dispose()
   {
      DisposeInternal();
#pragma warning disable CS0162 // Unreachable code detected
      if(ResourceLeakDebugMode)
      {
         UncatchableExceptionsGatherer.ForceFullGcAllGenerationsAndWaitForFinalizersConsumeAndThrowAnyGatheredExceptions();
      }
#pragma warning restore CS0162 // Unreachable code detected
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
