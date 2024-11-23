using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class TestController : Controller
{
   [HttpPost("/test")] public IActionResult GetTest([FromBody] MyQuery query)
   {
      var response = new MyResponse { Result = query.Input + " response" };
      return Ok(response);
   }
}

public class MyQuery
{
   public string Input { get; set; } = "";
}

public class MyResponse
{
   public string Result { get; set; } = "";
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
            var response = await client.PostAsJsonAsync(requestUri, new MyQuery { Input = "test" });
            var result = await response.Content.ReadFromJsonAsync<MyResponse>();
            Assert.That(result.NotNull().Result, Is.EqualTo("test response"));
         }
         finally
         {
            await server.StopAsync();
         }
      }));

      await Task.WhenAll(servers.Select(server => server.StopAsync()));
   }

   static void AddMvcServices(IServiceCollection services) { services.AddControllers(); }

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
