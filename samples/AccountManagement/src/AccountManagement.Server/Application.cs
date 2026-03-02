using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Threading.TasksCE;

// ReSharper disable LocalizableElement

namespace AccountManagement;

static class Application
{
   public static async Task Main()
   {
#pragma warning disable CA2000// We are tracking it for disposal on the very next line
      var host = EndpointHost.Production.Create(() => new SimpleInjectorDependencyInjectionContainer());
#pragma warning restore CA2000
      await using var _ = host.caf();
      AccountManagementServerDomainBootstrapper.RegisterWith(host);
      await host.StartAsync().caf();
      Console.WriteLine("Press enter to exit");
      Console.ReadLine();
   }
}
