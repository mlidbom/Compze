using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AccountManagement.UI.MVC;

static class Program
{
   public static void Main(string[] args) => BuildWebHost(args).Run();

   static IWebHost BuildWebHost(string[] args) =>
      WebHost.CreateDefaultBuilder(args)
             .UseStartup<Startup>()
             .Build();
}