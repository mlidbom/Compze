using System;
using System.Threading.Tasks;
using Compze.Tessaging.Buses;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable LocalizableElement

namespace AccountManagement;

static class Application
{
   public static async Task Main()
   {
      var host = EndpointHost.Production.Create(AccountManagementContainerFactory.Create);
      await using var host2 = host.caf();
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      await host.StartAsync().caf();
      Console.WriteLine("Press enter to exit");
      Console.ReadLine();
   }
}