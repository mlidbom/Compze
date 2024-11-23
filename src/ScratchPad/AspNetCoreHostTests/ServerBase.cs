using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ScratchPad.AspNetCoreHostTests;

public class TestObjectsController : Controller
{
   [HttpPost("/test/objects")] public IActionResult GetTest([FromBody] MyQuery query) =>
      Ok(new MyResponse { Result = query.Input + " response" });
}

public class TestStringsController : Controller
{
   [HttpPost("/test/strings")] public async Task<IActionResult> GetTest()
   {
      using var reader = new StreamReader(Request.Body);
      var query = await reader.ReadToEndAsync();
      return Ok($"{query} response");
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
   [Test] public async Task TestObjects()
   {
      var servers = await Task.WhenAll(1.Through(10).Select(_ => SetupWebApplication()));

      await Task.WhenAll(servers.Select(async server =>
      {
         try
         {
            using var client = server.Services.GetRequiredService<HttpClient>();

            async Task RunQuery()
            {
               var requestUri = $"{server.Urls.First()}/test/objects";
               var response = await client.PostAsJsonAsync(requestUri, new MyQuery { Input = "test" });
               var result = await response.Content.ReadFromJsonAsync<MyResponse>();
               Assert.That(result.NotNull().Result, Is.EqualTo("test response"));
            }
            await Task.WhenAll(1.Through(100).Select(_ => RunQuery()));
         }
         finally
         {
            await server.StopAsync();
         }
      }));

      await Task.WhenAll(servers.Select(server => server.StopAsync()));
   }

   [Test] public async Task TestStrings()
   {
      var servers = await Task.WhenAll(101.Through(110).Select(_ => SetupWebApplication()));

      await Task.WhenAll(servers.Select(async server =>
      {
         try
         {
            using var client = server.Services.GetRequiredService<HttpClient>();

            async Task RunQuery()
            {
               var requestUriStrings = $"{server.Urls.First()}/test/strings";
               var responseString = await client.PostAsync(requestUriStrings, new StringContent("test", Encoding.UTF8, "text/plain"));
               var resultString = await responseString.Content.ReadAsStringAsync();
               Assert.That(resultString.NotNull(), Is.EqualTo("test response"));
            }

            await Task.WhenAll(1.Through(100).Select(_ => RunQuery()));
         }
         finally
         {
            await server.StopAsync();
         }
      }));

      await Task.WhenAll(servers.Select(server => server.StopAsync()));
   }

   static void AddMvcServices(IServiceCollection services) { services.AddControllers(); }

   static async Task<WebApplication> SetupWebApplication()
   {
      var builder = WebApplication.CreateBuilder();
      builder.Logging.SetMinimumLevel(LogLevel.Warning);

      builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TestObjectsController).Assembly));
      builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TestStringsController).Assembly));

      builder.WebHost.UseUrls($"http://127.0.0.1:0");

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