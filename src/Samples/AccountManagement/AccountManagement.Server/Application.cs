using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.DependencyInjection.SimpleInjector;
using Compze.Tessaging.Buses;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable LocalizableElement

namespace AccountManagement;

static class Application
{
   public static async Task Main()
   {
      var host = EndpointHost.Production.Create(runMode =>
      {
         IDependencyInjectionContainer container = new SimpleInjectorDependencyInjectionContainer(runMode);
         container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
         return container;
      });
      await using var host2 = host.caf();
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      await host.StartAsync().caf();
      Console.WriteLine("Press enter to exit");
      Console.ReadLine();
   }
}