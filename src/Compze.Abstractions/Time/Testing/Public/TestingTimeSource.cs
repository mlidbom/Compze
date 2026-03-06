using System.Globalization;
using Compze.Abstractions.Time.Public;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.ThreadingCE;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Compze.Abstractions.Time.Testing.Public;

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

      public unit Run(Action action) => Run(action.ToFunc());

      public TResult Run<TResult>(Func<TResult> action) => RunAsync(action.AsAsync()).ResultUnwrappingException();

      public async Task<unit> RunAsync(Func<Task> action) => await RunAsync(action.ToAsyncFunc()).caf();

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
