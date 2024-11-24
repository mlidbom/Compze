using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
// ReSharper disable LocalizableElement

namespace AccountManagement;

public class Application
{
   public static async Task Main()
   {
      var host = EndpointHost.Production.Create(DependencyInjectionContainer.Create);
      await using var host2 = host;
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      await host.StartAsync();
      Console.WriteLine("Press enter to exit");
      Console.ReadLine();
   }
}