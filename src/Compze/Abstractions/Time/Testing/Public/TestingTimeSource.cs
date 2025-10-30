using System;
using System.Globalization;
using System.Threading.Tasks;
using Compze.Core.Time.Public;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Core.Time.Testing.Public;

public static class TestingTimeSource
{
   static IUtcTimeTimeSource FrozenUtcNow() => new ConstantTimeSource(DateTime.UtcNow);

   static IUtcTimeTimeSource FrozenUtc(DateTime utcTime) => new ConstantTimeSource(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc));

   static IUtcTimeTimeSource FrozenUtc(string time) => new ConstantTimeSource(DateTime.Parse(time, CultureInfo.InvariantCulture).ToUniversalTime());


   public static TimeSourceOverride FrozenAtUtcNow() => new TimeSourceOverride(FrozenUtcNow());
   public static TimeSourceOverride FrozenAtUtc(DateTime time) => new TimeSourceOverride(FrozenUtc(time));
   public static TimeSourceOverride FrozenAtUtc(string time) => new TimeSourceOverride(FrozenUtc(time));

   class ConstantTimeSource(DateTime time) : IUtcTimeTimeSource
   {
      public DateTime UtcNow { get; } = time;
   }

   public class TimeSourceOverride(IUtcTimeTimeSource theOverride)
   {
      readonly IUtcTimeTimeSource _theOverride = theOverride;

      public unit Run(Action action) => Run(action.AsFunc());

      public TResult Run<TResult>(Func<TResult> action) => RunAsync(action.AsAsync()).Result;

      public async Task<unit> RunAsync(Func<Task> action) => await RunAsync(action.AsFunc()).caf();

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
