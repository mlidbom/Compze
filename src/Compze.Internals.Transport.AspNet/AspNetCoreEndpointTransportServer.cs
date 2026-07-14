using System.Reflection;
using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Extensions.Hosting;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compze.Internals.Transport.AspNet;

public static class AspNetCoreEndpointTransportServerRegistrar
{
   ///<summary>Registers the ASP.NET Core implementation of the endpoint's one transport server (<see cref="IEndpointTransportServer"/>)<br/>
   /// together with the <see cref="EndpointDiscoveryQueryController"/> it hosts, unless a transport already registered one — guarded so<br/>
   /// that every communication style's ASP.NET Core transport registration can demand the server without conflicting when an endpoint<br/>
   /// hosts several styles.</summary>
   public static IComponentRegistrar AspNetCoreEndpointTransportServerIfNotRegistered(this IComponentRegistrar registrar) =>
      registrar.IsRegistered<IEndpointTransportServer>()
         ? registrar
         : registrar.Register(EndpointDiscoveryQueryController.RegisterWith)
                    .Register(
                       Singleton.For<IEndpointTransportServer>()
                                .CreatedBy((IChildContainerHostIntegration hostIntegration, IComponentSet<AspNetCoreControllerContribution> controllerContributions)
                                              => new AspNetCoreEndpointTransportServer(hostIntegration, controllerContributions)));
}

///<summary>The ASP.NET Core implementation of <see cref="IEndpointTransportServer"/>: one Kestrel <see cref="WebApplication"/> hosting<br/>
/// every communication style's contributed controllers, plus the <see cref="EndpointDiscoveryQueryController"/> answering the<br/>
/// endpoint-discovery queries — which every endpoint serves no matter what it speaks, so the server hosts<br/>
/// that controller itself.</summary>
///<remarks>Controllers are activated through the endpoint's own container (<see cref="IChildContainerHostIntegration"/> +<br/>
/// <see cref="ServiceBasedControllerActivator"/>), so they resolve the endpoint's services exactly as any other component does.</remarks>
class AspNetCoreEndpointTransportServer : IEndpointTransportServer
{
   readonly IChildContainerHostIntegration _hostIntegration;
   readonly IEnumerable<AspNetCoreControllerContribution> _controllerContributions;
   WebApplication? _webApplication;

   internal AspNetCoreEndpointTransportServer(IChildContainerHostIntegration hostIntegration, IEnumerable<AspNetCoreControllerContribution> controllerContributions)
   {
      _hostIntegration = hostIntegration;
      _controllerContributions = controllerContributions;
   }

   public EndpointAddress Address => new(new Uri(_webApplication._assert().NotNull().Urls.First()));

   public async Task StartAsync() => _webApplication = await StartServerAsync().caf();

   public async Task StopAsync()
   {
      if(_webApplication is null) return;
      await _webApplication.StopAsync().caf();
      await _webApplication.DisposeAsync().caf();
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
         it.ApplicationParts.Add(new AssemblyPart(typeof(EndpointDiscoveryQueryController).Assembly));
         foreach(var contribution in _controllerContributions)
            it.ApplicationParts.Add(new AssemblyPart(contribution.ControllerAssembly));
         it.FeatureProviders.Add(new InternalControllerFeatureProvider());
      });

      //todo: in production we want to bind to a specific configured port, not a random one.
      builder.WebHost.UseUrls("http://127.0.0.1:0");

      builder.Services.AddHttpClient();
      builder.Services.AddControllers();

      builder.Services.AddSingleton<IControllerActivator, ServiceBasedControllerActivator>();

      _hostIntegration.UseChildContainerAsServiceProviderFor(builder.Host);

      var app = builder.Build();

      app.UseRouting();
      app.MapControllers();

      await app.StartAsync().caf();

      return app;
   }

   ///<summary>ASP.NET Core's default controller discovery only sees public types; Compze's controllers are internal by design, so<br/>
   /// discovery is widened to every <see cref="Controller"/> subclass in the hosted assemblies regardless of visibility.</summary>
   class InternalControllerFeatureProvider : ControllerFeatureProvider
   {
      protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
   }
}
