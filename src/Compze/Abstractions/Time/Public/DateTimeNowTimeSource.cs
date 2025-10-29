using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

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


   public static TResult WithOverride<TResult>(IUtcTimeTimeSource theOverride, Func<TResult> action)
   {
      using(ScopedChange.Enter(() => Override.Value = theOverride, () => Override.Value = null))
      {
         return action();
      }
   }

   public static async Task<TResult> WithOverrideAsync<TResult>(IUtcTimeTimeSource theOverride, Func<Task<TResult>> action)
   {
      using(ScopedChange.Enter(() => Override.Value = theOverride, () => Override.Value = null))
      {
         return await action();
      }
   }


   public static async Task WithOverrideAsync(IUtcTimeTimeSource theOverride, Func<Task> action)
   {
      using(ScopedChange.Enter(() => Override.Value = theOverride, () => Override.Value = null))
      {
         await action();
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