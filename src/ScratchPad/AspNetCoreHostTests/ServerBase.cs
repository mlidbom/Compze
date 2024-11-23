using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Composable.SystemCE.LinqCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ScratchPad.AspNetCoreHostTests;

public class TestAspNetCoreHost
{
   [Test] public async Task RunServer()
   {
      var servers = await Task.WhenAll(1.Through(100).Select(SetupWebApplication));

      await Task.WhenAll(servers.Select(async server =>
      {
         try
         {
            using var client = server.Services.GetRequiredService<HttpClient>();
            var requestUri = $"{server.Urls.First()}/test";
            Assert.That(await client.GetStringAsync(requestUri), Is.EqualTo("test"));
         }
         finally
         {
            await server.StopAsync();
         }
      }));

      await Task.WhenAll(servers.Select(server => server.StopAsync()));
   }

   static async Task<WebApplication> SetupWebApplication(int portOffset)
   {
      var WebAppBuilder = WebApplication.CreateBuilder();

      WebAppBuilder.WebHost.UseUrls($"http://localhost:{5500 + portOffset}");

      WebAppBuilder.Services.AddHttpClient();
      var serviceProvider = WebAppBuilder.Services.BuildServiceProvider();

      var webApp = WebAppBuilder.Build();

      webApp.MapGet("/test", () => "test");

      await webApp.StartAsync();
      return webApp;
   }
}
