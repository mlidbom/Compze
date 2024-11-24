using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Composable.Messaging.Buses.Implementation;

public class InternalControllerFeatureProvider : ControllerFeatureProvider
{
   protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(QueryController);
}
partial class Inbox
{
   class AspNetHost : IAsyncDisposable
   {
      readonly HandlerExecutionEngine _handlerExecutionEngine;
      readonly ITypeMapper _typeMapper;
      readonly IRemotableMessageSerializer _serializer;
      WebApplication? _webApplication;

      [SuppressMessage("ReSharper", "UnusedParameter.Local")]
      internal AspNetHost(HandlerExecutionEngine handlerExecutionEngine, IMessageStorage storage, string address, RealEndpointConfiguration configuration, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         _handlerExecutionEngine = handlerExecutionEngine;
         _typeMapper = typeMapper;
         _serializer = serializer;
      }
      public string Address => _webApplication!.Urls.First();

      public async Task StartAsync() { _webApplication = await StartServerAsync().NoMarshalling(); }

      public async Task StopAsync()
      {
         if(_webApplication is null) return;
         await _webApplication!.StopAsync().NoMarshalling();
         _webApplication = null;
      }

      public async ValueTask DisposeAsync() => await StopAsync().NoMarshalling();

      async Task<WebApplication> StartServerAsync()
      {
         var builder = WebApplication.CreateBuilder();
         builder.Logging.SetMinimumLevel(LogLevel.Warning);

         builder.Services.AddControllers().ConfigureApplicationPartManager(it =>
         {
            it.ApplicationParts.Add(new AssemblyPart(typeof(QueryController).Assembly));
            it.FeatureProviders.Add(new InternalControllerFeatureProvider());
         });

         builder.WebHost.UseUrls("http://127.0.0.1:0");

         builder.Services.AddHttpClient();
         builder.Services.AddControllers();

         builder.Services.AddSingleton(_ => _serializer);
         builder.Services.AddSingleton(_ => _typeMapper);
         builder.Services.AddSingleton(_ => _handlerExecutionEngine);

         var app = builder.Build();

         app.UseRouting();
         app.MapControllers();

         await app.StartAsync().NoMarshalling();
         return app;
      }
   }
}
class QueryController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine) : Controller
{
   [HttpPost("/internal/rpc/query")] public async Task<IActionResult> Query()
   {
      var messageId = Guid.Parse(HttpContext.Request.Headers["MessageId"][0].NotNull());
      var typeIdStr = HttpContext.Request.Headers["PayloadTypeId"][0].NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(Request.Body);
      var queryJson = await reader.ReadToEndAsync().NoMarshalling();

      var transportMessage = new TransportMessage.InComing(queryJson, typeId, [], messageId, typeMapper, serializer);

      try
      {
         var queryResultObject = (await handlerExecutionEngine.Enqueue(transportMessage).NoMarshalling()).NotNull();
         var responseJson = serializer.SerializeResponse(queryResultObject);
         return Ok(responseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type:exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
