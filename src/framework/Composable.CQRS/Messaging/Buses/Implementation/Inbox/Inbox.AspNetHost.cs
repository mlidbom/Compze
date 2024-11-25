using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
      readonly IServiceLocator _serviceLocator;
      readonly IDependencyInjectionContainer _container;
      WebApplication? _webApplication;

      [SuppressMessage("ReSharper", "UnusedParameter.Local")]
      internal AspNetHost(HandlerExecutionEngine handlerExecutionEngine, IMessageStorage storage, string address, RealEndpointConfiguration configuration, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IServiceLocator serviceLocator, IDependencyInjectionContainer container)
      {
         _handlerExecutionEngine = handlerExecutionEngine;
         _typeMapper = typeMapper;
         _serializer = serializer;
         _serviceLocator = serviceLocator;
         _container = container;
      }
      public string Address => _webApplication!.Urls.First();

      public async Task StartAsync() => _webApplication = await StartServerAsync().NoMarshalling();

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

         _container.RegisterServicesInIServiceCollection(builder.Services);

         var app = builder.Build();

         app.UseRouting();
         app.MapControllers();

         app.Use((_, next) => _serviceLocator.ExecuteInIsolatedScopeAsync(next.Invoke));

         await app.StartAsync().NoMarshalling();
         return app;
      }
   }
}
