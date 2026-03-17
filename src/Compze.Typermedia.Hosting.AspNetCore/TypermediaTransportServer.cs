using System.Reflection;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IServiceScope = Compze.DependencyInjection.Abstractions.IServiceScope;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compze.Typermedia.Hosting.AspNetCore;

public class TypermediaTransportServer : ITypermediaTransportServer
{
   const string CompzeScopeHttpContextItemKey = "CompzeScope";

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

      builder.Services.AddSingleton<IControllerActivator>(new ScopeFromHttpContextControllerActivator());

      var app = builder.Build();

      app.UseRouting();
      app.MapControllers();

      // Create a scope in our container for each request and store it in HttpContext.Items
      app.Use(async (httpContext, next) =>
      {
         using var scope = _serviceLocator.BeginScope();
         httpContext.Items[CompzeScopeHttpContextItemKey] = scope;
         await next.Invoke().caf();
      });

      await app.StartAsync().caf();

      return app;
   }

   class ScopeFromHttpContextControllerActivator : IControllerActivator
   {
      public object Create(ControllerContext context)
      {
         var scope = (IServiceScope)context.HttpContext.Items[CompzeScopeHttpContextItemKey]!;
         return scope.Resolve(context.ActionDescriptor.ControllerTypeInfo.AsType());
      }

      public void Release(ControllerContext context, object controller) {}
   }

   class InternalControllerFeatureProvider : ControllerFeatureProvider
   {
      protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
   }
}
