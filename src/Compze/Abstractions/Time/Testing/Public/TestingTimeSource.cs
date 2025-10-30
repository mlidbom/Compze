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

/// <summary> Just statically returns whatever value was assigned.</summary>
public class TestingTimeSource : IUtcTimeTimeSource
{
   DateTime? _freezeAt;

   TestingTimeSource() {}

   ///<summary>Returns a time source that will continually return the time that it was created at as the current time.</summary>
   public static TestingTimeSource FrozenUtcNow() => new()
                                                     {
                                                        _freezeAt = DateTime.UtcNow
                                                     };

   ///<summary>Returns a time source that will forever return <param name="utcTime"> as the current time.</param></summary>
   public static TestingTimeSource FrozenUtc(DateTime utcTime) => new()
                                                                        {
                                                                           _freezeAt = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc)
                                                                        };

   public static TestingTimeSource FrozenUtc(string time) => FrozenUtc(DateTime.Parse(time, CultureInfo.InvariantCulture).ToUniversalTime());


   ///<summary>Gets the current UTC time.</summary>
   public DateTime UtcNow => _freezeAt ?? DateTimeNowTimeSource.Instance.UtcNow;



   public TimeSourceOverride FrozenAtUtcNow() => new TimeSourceOverride(FrozenUtcNow());
   public TimeSourceOverride FrozenAtUtc(DateTime time) => new TimeSourceOverride(FrozenUtc(time));
   public TimeSourceOverride FrozenAtUtc(string time) => new TimeSourceOverride(FrozenUtc(time));

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
