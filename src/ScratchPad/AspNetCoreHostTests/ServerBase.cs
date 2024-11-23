using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Composable.SystemCE.LinqCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class TestController : Controller
{
   [HttpGet("/test")]
   public IActionResult GetTest()
   {
       return Ok("test");
   }
}
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

   static void AddMvcServices(IServiceCollection services)
   {
       services.AddControllers();
   }

   static async Task<WebApplication> SetupWebApplication(int portOffset)
   {
      var builder = WebApplication.CreateBuilder();
      builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TestController).Assembly)); // Necessary to add controllers from other projects


      builder.WebHost.UseUrls($"http://localhost:{5500 + portOffset}");

      builder.Services.AddHttpClient();
      builder.Services.AddControllers();
      AddMvcServices(builder.Services);
      var app = builder.Build();

      app.UseRouting();
      app.MapControllers();

      await app.StartAsync();
      return app;
   }
}
