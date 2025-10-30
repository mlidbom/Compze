using System;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

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
