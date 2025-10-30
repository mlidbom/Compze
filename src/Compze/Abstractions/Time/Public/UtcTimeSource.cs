using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Core.Time.Public;

public static class UtcTimeSource
{
   static readonly IUtcTimeTimeSource TimeSource = DateTimeNowTimeSource.Instance;

   public static DateTime UtcNow => Override?.Value?.UtcNow ?? TimeSource.UtcNow;

   internal static readonly ThreadLocal<IUtcTimeTimeSource?> Override = new();

   public static unit WithOverride(IUtcTimeTimeSource theOverride, Action action) =>
      WithOverride(theOverride, action.AsFunc());

   public static TResult WithOverride<TResult>(IUtcTimeTimeSource theOverride, Func<TResult> action) =>
      WithOverrideAsync(theOverride, action.AsAsync()).Result;

   public static async Task<unit> WithOverrideAsync(IUtcTimeTimeSource theOverride, Func<Task> action) =>
      await WithOverrideAsync(theOverride, action.AsFunc()).caf();

   public static async Task<TResult> WithOverrideAsync<TResult>(IUtcTimeTimeSource theOverride, Func<Task<TResult>> action)
   {
      var current = Override.Value;
      using(ScopedChange.Enter(() => Override.Value = theOverride, () => Override.Value = current))
      {
         return await action().caf();
      }
   }
}
