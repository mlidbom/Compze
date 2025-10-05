using Compze.DependencyInjection;
using Compze.DependencyInjection.SimpleInjector;

namespace AccountManagement;

/// <summary>
/// Factory for creating DI containers for the AccountManagement application.
/// In production, applications typically standardize on a single DI container implementation.
/// This sample uses SimpleInjector.
/// </summary>
public static class AccountManagementContainerFactory
{
   public static IDependencyInjectionContainer Create(IRunMode runMode)
   {
      IDependencyInjectionContainer container = new SimpleInjectorDependencyInjectionContainer(runMode);
      container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
      return container;
   }
}
