using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

public class AspNetInboxTransportServer : IInboxTransportServer
{
   readonly IServiceLocator _serviceLocator;
   WebApplication? _webApplication;
   readonly CompzeControllerActivator _controllerActivator;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IInboxTransportServer>()
                  .CreatedBy((IServiceLocator serviceLocator, CompzeControllerActivator activator)
                                => new AspNetInboxTransportServer(serviceLocator, activator)));

   AspNetInboxTransportServer(IServiceLocator serviceLocator, CompzeControllerActivator controllerActivator)
   {
      _controllerActivator = controllerActivator;
      _serviceLocator = serviceLocator;
   }

   public Uri Address => new(_webApplication!.Urls.First());

   public async Task StartAsync() => _webApplication = await StartServerAsync().caf();

   public async Task StopAsync()
   {
      if(_webApplication is null) return;
      await _webApplication.StopAsync().caf();
      _webApplication = null;
   }

   public async ValueTask DisposeAsync() => await StopAsync().caf();

   async Task<WebApplication> StartServerAsync()
   {
      var builder = WebApplication.CreateBuilder();
      //todo: hardcoding the log level should be changed
      builder.Logging.SetMinimumLevel(LogLevel.Warning);
      //todo: hardcoding logging to a local Seq should be changed.
      builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSeq(serverUrl: "http://192.168.0.11:5341"));

      builder.Services.AddControllers().ConfigureApplicationPartManager(it =>
      {
         it.ApplicationParts.Add(new AssemblyPart(GetType().Assembly));
         it.FeatureProviders.Add(new InternalControllerFeatureProvider());
      });

      //todo: in production we want to bind to a specific configured port, not a random one.
      builder.WebHost.UseUrls("http://127.0.0.1:0");

      builder.Services.AddHttpClient();
      builder.Services.AddControllers();

      //We need to use our container or things go haywire.
      builder.Services.AddSingleton<IControllerActivator>(_controllerActivator);

      var app = builder.Build();

      app.UseRouting();
      app.MapControllers();

      // Create a scope in our container for each request
      app.Use((_, next) => _serviceLocator.ExecuteInIsolatedScopeAsync(next.Invoke));

      await app.StartAsync().caf();

      return app;
   }
}
