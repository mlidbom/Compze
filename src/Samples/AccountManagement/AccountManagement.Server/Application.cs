using System;
using System.Threading.Tasks;
using Compze.Messaging.Buses;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing.DependencyInjection;

// ReSharper disable LocalizableElement

namespace AccountManagement;

static class Application
{
   public static async Task Main()
   {
      var host = EndpointHost.Production.Create(TestingContainerFactory.Create);
      await using var host2 = host.CaF();
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      await host.StartAsync().CaF();
      Console.WriteLine("Press enter to exit");
      Console.ReadLine();
   }
}