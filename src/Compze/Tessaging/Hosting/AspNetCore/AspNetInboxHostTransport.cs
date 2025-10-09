using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compze.Tessaging.Hosting.AspNetCore;

class InternalControllerFeatureProvider : ControllerFeatureProvider
{
   protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
}


public class AspNetInboxTransport : IInboxTransport
{
   readonly IServiceLocator _serviceLocator;
   readonly IDependencyInjectionContainer _container;
   WebApplication? _webApplication;

   public AspNetInboxTransport(IServiceLocator serviceLocator, IDependencyInjectionContainer container)
   {
      _serviceLocator = serviceLocator;
      _container = container;
   }

   public string Address => _webApplication!.Urls.First();

   public async Task StartAsync() => _webApplication = await StartServerAsync().caf();

   public async Task StopAsync()
   {
      if(_webApplication is null) return;
      await _webApplication.StopAsync().caf();
      _webApplication = null;
   }

   public async ValueTask DisposeAsync()
   {
      await StopAsync().caf();
      GC.SuppressFinalize(this);
   }

   async Task<WebApplication> StartServerAsync()
   {
      var builder = WebApplication.CreateBuilder();
      builder.Logging.SetMinimumLevel(LogLevel.Warning);
      builder.Services.AddLogging(something => something.AddSeq(serverUrl: "http://192.168.0.11:5341"));

      builder.Services.AddControllers().ConfigureApplicationPartManager(it =>
      {
         it.ApplicationParts.Add(new AssemblyPart(GetType().Assembly));
         it.FeatureProviders.Add(new InternalControllerFeatureProvider());
      });

      builder.WebHost.UseUrls("http://127.0.0.1:0");

      builder.Services.AddHttpClient();
      builder.Services.AddControllers();

      //We need to use our container or things go haywire.
      builder.Services.AddSingleton<IControllerActivator>(new CompzeControllerActivator(_serviceLocator));

      var app = builder.Build();

      app.UseRouting();
      app.MapControllers();

      // Create a scope in our container for each request
      app.Use((_, next) => _serviceLocator.ExecuteInIsolatedScopeAsync(next.Invoke));

      await app.StartAsync().caf();

      return app;
   }
}
