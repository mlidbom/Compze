using Compze.Abstractions.Internal.Time;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Wiring;

internal static class UniversalWiring
{
   public static IDependencyRegistrar TimeSource(this IDependencyRegistrar @this)
   {
      if(@this.RunMode == RunMode.Production)
      {
         return @this.Register(Singleton.For<IUtcTimeTimeSource>()
                                        .CreatedBy(() => new DateTimeNowTimeSource())
                                        .DelegateToParentServiceLocatorWhenCloning());
      } else
      {
         return @this.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>()
                                        .CreatedBy(() => TestingTimeSource.FollowingSystemClock)
                                        .DelegateToParentServiceLocatorWhenCloning());
      }
   }
}