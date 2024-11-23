using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Composable.Messaging.Buses.Implementation;

partial class Inbox
{
   class AspNetHost : IAsyncDisposable
   {
      WebApplication? _webApplication;


      [SuppressMessage("ReSharper", "UnusedParameter.Local")] internal AspNetHost(HandlerExecutionEngine handlerExecutionEngine, IMessageStorage storage, string address, RealEndpointConfiguration configuration, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
      }

      public async Task StartAsync()
      {
         _webApplication = await StartServerAsync().NoMarshalling();
      }

      public async Task StopAsync()
      {
         if (_webApplication is null) return;
         await _webApplication!.StopAsync().NoMarshalling();
         _webApplication = null;
      }

      public async ValueTask DisposeAsync() => await StopAsync().NoMarshalling();

      static async Task<WebApplication> StartServerAsync()
      {
         var builder = WebApplication.CreateBuilder();
         builder.Logging.SetMinimumLevel(LogLevel.Warning);

         builder.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(QueryController).Assembly));

         builder.WebHost.UseUrls("http://127.0.0.1:0");

         builder.Services.AddHttpClient();
         builder.Services.AddControllers();
         var app = builder.Build();

         app.UseRouting();
         app.MapControllers();

         await app.StartAsync().NoMarshalling();
         return app;
      }

      protected class QueryController : Controller
      {
         [HttpPost("/internal/rpc/query")] public async Task<IActionResult> Query()
         {
            using var reader = new StreamReader(Request.Body);
            var query = await reader.ReadToEndAsync().NoMarshalling();
            return Ok($"{query} response");
         }
      }
   }
}