namespace Compze.Tests.Infrastructure;

///<summary>Observing, in an async specification, whether a task completes in time, without the observation itself failing the specification.</summary>
public static class SpecificationTaskCE
{
   extension(Task @this)
   {
      ///<summary>True if this task completes, however it completes, within <paramref name="patience"/>. Never throws what the task completes with.</summary>
      public async Task<bool> CompletesWithinAsync(TimeSpan patience) => await Task.WhenAny(@this, Task.Delay(patience)) == @this;

      ///<summary>Awaits this task's completion, however it completes, within <paramref name="patience"/>: a failure or cancellation is an outcome the<br/>
      /// specification asserts on afterwards, not one this throws. Throws a <see cref="TimeoutException"/> if the task does not complete in time.</summary>
      public async Task AwaitCompletionWithinAsync(TimeSpan patience)
      {
         if(!await @this.CompletesWithinAsync(patience)) throw new TimeoutException($"The task did not complete within {patience}.");
      }
   }
}
