using Compze.Abstractions.Internal.Time;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Wiring;

internal static class UniversalWiring
{
   public static void RegisterTimeSource(this IDependencyInjectionContainer @this)
   {
      // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
      if(@this.RunMode == RunMode.Production)
      {
         @this.Register(Singleton.For<IUtcTimeTimeSource>().CreatedBy(() => new DateTimeNowTimeSource()).DelegateToParentServiceLocatorWhenCloning());
      } else
      {
         @this.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>().CreatedBy(() => TestingTimeSource.FollowingSystemClock).DelegateToParentServiceLocatorWhenCloning());
      }
   }
}
internal static class TransportWiring
{
   
}

internal static class ClientWiring
{
   
}

internal static class HostWiring
{
   
}
