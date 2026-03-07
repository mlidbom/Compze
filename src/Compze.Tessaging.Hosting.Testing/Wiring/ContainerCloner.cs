using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class ContainerCloner
{
   public static IServiceLocator Clone(this IServiceLocator @this)
   {
      var cloneContainer = ((IDependencyInjectionContainer)@this).Clone();

      cloneContainer.Register(Singleton.For<IDependencyInjectionContainer>().Instance(cloneContainer));

      return cloneContainer.ServiceLocator;
   }
}
