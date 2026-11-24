using System.Globalization;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Abstractions.Time.Testing;

public class TestingTimeSourceAdapter
{
   internal static readonly TestingTimeSourceAdapter Instance  = new ();

   TestingTimeSourceAdapter(){}

   IUtcTimeTimeSource FrozenUtcNow() => new ConstantTimeSource(DateTime.UtcNow);

   IUtcTimeTimeSource FrozenUtc(DateTime utcTime) => new ConstantTimeSource(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc));

   IUtcTimeTimeSource FrozenUtc(string time) => new ConstantTimeSource(DateTime.Parse(time, CultureInfo.InvariantCulture).ToUniversalTime());


   public TimeSourceOverride FrozenAtUtcNow() => new(FrozenUtcNow());
   public TimeSourceOverride FrozenAtUtc(DateTime time) => new(FrozenUtc(time));
   public TimeSourceOverride FrozenAtUtc(string time) => new(FrozenUtc(time));

   class ConstantTimeSource(DateTime time) : IUtcTimeTimeSource
   {
      public DateTime UtcNow { get; } = time.TruncateToMicroseconds(); //Some of our supported databases only have microsecond precision, and we compare the entire contents of our objects in tests.
    }

   public class TimeSourceOverride(IUtcTimeTimeSource theOverride)
   {
      readonly IUtcTimeTimeSource _theOverride = theOverride;

      public Unit Run(Action action) => Run(action.ToFunc());

      public TResult Run<TResult>(Func<TResult> action)
      {
         //todo:urgent: a synchronous wait on asynchronous work, through ResultUnwrappingException, which VSTHRD002 can't see: it
         //   holds this thread, and stalls while the thread pool is busy. See
         //   dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md (Magnus, 2026-11-24).
         return RunAsync(action.ToCompletedTaskFunc()).ResultUnwrappingException();
      }

      public async Task<Unit> RunAsync(Func<Task> action) => await RunAsync(action.ToAsyncFunc()).caf();

      public async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> action)
      {
         var current = UtcTimeSource.Override.Value;
         using(ScopedChange.Enter(() => UtcTimeSource.Override.Value = _theOverride, () => UtcTimeSource.Override.Value = current))
         {
            return await action().caf();
         }
      }
   }

}
