using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class ContainerCloner
{
   public static IServiceLocator Clone(this IServiceLocator @this)
   {
      var cloneContainer = ((ILegacyContainer)@this).Clone();

      cloneContainer.Register(Singleton.For<ILegacyContainer>().Instance(cloneContainer));

      return cloneContainer.ServiceLocator;
   }
}
