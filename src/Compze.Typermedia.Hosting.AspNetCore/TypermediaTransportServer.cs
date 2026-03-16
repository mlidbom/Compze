using System.Reflection;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compze.Typermedia.Hosting.AspNetCore;

public class TypermediaTransportServer : ITypermediaTransportServer
{
   readonly IServiceLocator _serviceLocator;
   WebApplication? _webApplication;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<ITypermediaTransportServer>()
                  .CreatedBy((IServiceLocator serviceLocator) => new TypermediaTransportServer(serviceLocator)));

   TypermediaTransportServer(IServiceLocator serviceLocator) => _serviceLocator = serviceLocator;

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
      builder.Logging.SetMinimumLevel(LogLevel.Warning);
      builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSeq(serverUrl: "http://192.168.0.11:5341"));

      builder.Services.AddControllers().ConfigureApplicationPartManager(it =>
      {
         it.ApplicationParts.Add(new AssemblyPart(typeof(TypermediaController).Assembly));
         it.ApplicationParts.Add(new AssemblyPart(typeof(Internals.Transport.AspNet.InfrastructureQueryController).Assembly));
         it.FeatureProviders.Add(new InternalControllerFeatureProvider());
      });

      builder.WebHost.UseUrls("http://127.0.0.1:0");

      builder.Services.AddHttpClient();
      builder.Services.AddControllers();

      builder.Services.AddSingleton<IControllerActivator>(new ServiceLocatorControllerActivator(_serviceLocator));

      var app = builder.Build();

      app.UseRouting();
      app.MapControllers();

      app.Use((_, next) => _serviceLocator.ExecuteInIsolatedScopeAsync(_ => next.Invoke()));

      await app.StartAsync().caf();

      return app;
   }

   class ServiceLocatorControllerActivator(IServiceLocator serviceLocator) : IControllerActivator
   {
      readonly IServiceLocator _serviceLocator1 = serviceLocator;

      public object Create(ControllerContext context) => _serviceLocator1.Resolve(context.ActionDescriptor.ControllerTypeInfo.AsType());
      public void Release(ControllerContext context, object controller) {}
   }

   class InternalControllerFeatureProvider : ControllerFeatureProvider
   {
      protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
   }
}
