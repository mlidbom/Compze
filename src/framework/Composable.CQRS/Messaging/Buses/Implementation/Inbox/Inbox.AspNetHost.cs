using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Composable.Messaging.Buses.Implementation;

class InternalControllerFeatureProvider : ControllerFeatureProvider
{
   protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
}

partial class Inbox
{
   internal class AspNetHost : IAsyncDisposable
   {
      readonly IServiceLocator _serviceLocator;
      readonly IDependencyInjectionContainer _container;
      WebApplication? _webApplication;

      internal AspNetHost(IServiceLocator serviceLocator, IDependencyInjectionContainer container)
      {
         _serviceLocator = serviceLocator;
         _container = container;
      }

      public string Address => _webApplication!.Urls.First();

      public async Task StartAsync() => _webApplication = await StartServerAsync().CaF();

      public async Task StopAsync()
      {
         if(_webApplication is null) return;
         await _webApplication.StopAsync().CaF();
         _webApplication = null;
      }

      public async ValueTask DisposeAsync() => await StopAsync().CaF();

      async Task<WebApplication> StartServerAsync()
      {
         var builder = WebApplication.CreateBuilder();
         builder.Logging.SetMinimumLevel(LogLevel.Information);

         builder.Services.AddLogging(something => something.AddSeq(serverUrl: "http://192.168.0.11:5341"));

         builder.Services.AddControllers().ConfigureApplicationPartManager(it =>
         {
            it.ApplicationParts.Add(new AssemblyPart(GetType().Assembly));
            it.FeatureProviders.Add(new InternalControllerFeatureProvider());
         });

         builder.WebHost.UseUrls("http://127.0.0.1:0");

         builder.Services.AddHttpClient();
         builder.Services.AddControllers();

         _container.RegisterToHandleServiceResolutionFor(builder.Services);

         var app = builder.Build();

         app.UseRouting();
         app.MapControllers();

         app.Services.AssertAllControllersCanBeInstantiated(_serviceLocator);

         app.Use((_, next) => _serviceLocator.ExecuteInIsolatedScopeAsync(next.Invoke));

         await app.StartAsync().CaF();

         return app;
      }
   }
}
