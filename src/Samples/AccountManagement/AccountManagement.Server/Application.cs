using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable LocalizableElement

namespace AccountManagement;

static class Application
{
   public static async Task Main()
   {
      var host = EndpointHost.Production.Create(DependencyInjectionContainer.Create);
      await using var host2 = host.CaF();
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      await host.StartAsync().CaF();
      Console.WriteLine("Press enter to exit");
      Console.ReadLine();
   }
}