using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Core.Time.Public;

static class TimeSourceRegistrar
{
   internal interface ITestingRegistrar
   {
      IComponentRegistrar Register();
   }

   public static IComponentRegistrar TimeSource(this IComponentRegistrar registrar)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register();
      } else
      {
         return DateTimeNowTimeSource.RegisterWith(registrar);
      }
   }
}

public static class UtcTimeSource
{
   static readonly IUtcTimeTimeSource TimeSource = DateTimeNowTimeSource.Instance;

   public static DateTime UtcNow => Override?.Value?.UtcNow ?? TimeSource.UtcNow;

   static readonly ThreadLocal<IUtcTimeTimeSource?> Override = new();

   public static unit WithOverride<TResult>(IUtcTimeTimeSource theOverride, Action action) =>
      WithOverride(theOverride, action.AsUnitFunc());

   public static TResult WithOverride<TResult>(IUtcTimeTimeSource theOverride, Func<TResult> action) =>
      WithOverrideAsync(theOverride, action.AsAsync()).Result;

   public static async Task<unit> WithOverrideAsync(IUtcTimeTimeSource theOverride, Func<Task> action) =>
      await WithOverrideAsync(theOverride, action.AsUnitFunc());

   public static async Task<TResult> WithOverrideAsync<TResult>(IUtcTimeTimeSource theOverride, Func<Task<TResult>> action)
   {
      var current = Override.Value;
      using(ScopedChange.Enter(() => Override.Value = theOverride, () => Override.Value = current))
      {
         return await action().caf();
      }
   }
}

///<summary>Simply returns DateTime.Now or DateTime.UtcNow</summary>
public class DateTimeNowTimeSource : IUtcTimeTimeSource
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar register)
      => register.Register(Singleton.For<IUtcTimeTimeSource>()
                                    .CreatedBy(() => new DateTimeNowTimeSource()));

   ///<summary>Returns an instance.</summary>
   public static readonly DateTimeNowTimeSource Instance = new();

   ///<summary>Returns DateTime.UtcNow</summary>
   public DateTime UtcNow => DateTime.UtcNow;
}
