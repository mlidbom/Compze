using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.DocumentDb.MicrosoftSql.Wiring;
using Compze.Hosting;
using Compze.Hosting.Configuration;
using Compze.Hosting.SameMachine;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.MicrosoftSql.Wiring;
using Compze.Tessaging.Transport.AspNetCore;
using Compze.Tessaging.Typermedia.Client;
using Compze.Teventive.TeventStore.MicrosoftSql.Wiring;
using Compze.Underscore;
using JetBrains.Annotations;
using static AccountManagement.AccountManagementServerDomainBootstrapper;

namespace AccountManagement.UI.MVC;

//todo:urgent: Creating endpoints, starting them up, and connecting a client to them.... We need a fundamental restructuring to make this sane.
///<summary>The production composition of the AccountManagement system, all in this one process: the domain's two endpoints on<br/>
/// an <see cref="EndpointHost"/> — each on its own Microsoft DI container, speaking the ASP.NET Core endpoint transport,<br/>
/// serializing with Newtonsoft, joining the one MsSql domain database (connection string in appsettings.json) — and the MVC<br/>
/// frontend navigating them as a pure <see cref="TypermediaClient"/>, exactly as it would from a separate process.</summary>
public class Startup
{
   ///<summary>The one domain database both endpoints join — its connection string lives in appsettings.json under this name.</summary>
   const string DomainDatabaseConnectionStringName = "AccountManagementDomain";

   readonly InterprocessEndpointRegistry _endpointRegistry;
   readonly IEndpointHost _host;
   readonly ExactlyOnceEndpoint _domainEndpoint;
   TypermediaClient _client = null!;

   public Startup(IConfiguration configuration)
   {
      Configuration = configuration;

      //The same-machine endpoint registry through which the endpoints of this process discover each other - exactly as
      //endpoints in separate processes on this machine would.
      _endpointRegistry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(
         "AccountManagement",
         new DirectoryInfo(Path.Combine(Path.GetTempPath(), "AccountManagement", "EndpointRegistry"))._mutate(it => it.Create()));

      _host = EndpointHost.Production.Create(CreateEndpointContainerBuilder);

      _domainEndpoint = _host.RegisterEndpoint(container => ExactlyOnceEndpoint.Build(
         container, DomainEndpointName, DomainEndpointId,
         endpointBuilder =>
         {
            DeclareProductionEnvironment(endpointBuilder);
            //The statistics endpoint's query models update from this endpoint's tevents. Requiring the peer makes the bus
            //queue its tevents for it even before first contact, so traffic can open without awaiting discovery.
            endpointBuilder.RequirePeers(StatisticsEndpointId);
            DeclareDomainEndpoint(endpointBuilder);
         }));

      _host.RegisterEndpoint(container => ExactlyOnceEndpoint.Build(
         container, StatisticsEndpointName, StatisticsEndpointId,
         endpointBuilder =>
         {
            DeclareProductionEnvironment(endpointBuilder);
            DeclareStatisticsEndpoint(endpointBuilder);
         }));
   }

   static IContainerBuilder CreateEndpointContainerBuilder()
   {
      var containerBuilder = new MicrosoftContainerBuilder();
      //The host integration lets the ASP.NET Core transport server use the endpoint's container as its service provider.
      MicrosoftChildContainerHostIntegration.RegisterWith(containerBuilder.Registrar);
      return containerBuilder;
   }

   ///<summary>The environment every endpoint of this composition runs in: the ASP.NET Core endpoint transport, Newtonsoft as<br/>
   /// the endpoint's one serializer and as the stores' persisted format, the MsSql domain database with its sql layers, the<br/>
   /// same-machine registry, and the configuration file the connection string is read from.</summary>
   void DeclareProductionEnvironment(ExactlyOnceEndpointBuilder endpointBuilder) =>
      endpointBuilder
         .TransportProtocol(registrar => registrar.AspNetCoreEndpointTransport())
         .NewtonsoftSerializer()
         .ConfigurePersistence(registrar => registrar.MsSqlDomainDatabase(DomainDatabaseConnectionStringName)
                                                     .MsSqlTessagingSqlLayer()
                                                     .MsSqlDocumentDbSqlLayer()
                                                     .MsSqlTeventStoreSqlLayer())
         .ParticipateIn(_endpointRegistry)
         .RegisterComponents(registrar => registrar.NewtonsoftDocumentDbSerializer()
                                                   .NewtonsoftTeventStoreSerializer()
                                                   .JSonAppConfigFileConfigurationParameterProvider());

   // ReSharper disable once MemberCanBePrivate.Global
   public IConfiguration Configuration { [UsedImplicitly] get; }

   // This method gets called by the runtime. Use this method to add services to the container.
   [UsedImplicitly] public void ConfigureServices(IServiceCollection services)
   {
      services.AddMvc();

      _host.Start();

      //The frontend is a pure client in its own container: it speaks typermedia to the domain endpoint's address, exactly
      //as it would from a separate process.
      _client = TypermediaClient.Build(new MicrosoftContainerBuilder(),
                                       clientBuilder =>
                                       {
                                          clientBuilder.ConfigureTransport(registrar => registrar.AspNetCoreEndpointTransportClient())
                                                       .DeclareRequiredTypeMappings(registrar => registrar.RequireAccountManagementTypeMappings());
                                          clientBuilder.NewtonsoftSerializer();
                                       });
      _client.ConnectAsync(_domainEndpoint.Address!).GetAwaiter().GetResult();

      services.AddHttpContextAccessor();
      services.AddScoped(_ => _client.Navigator);
   }

   // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
   [UsedImplicitly] public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
      if (env.IsDevelopment())
      {
         app.UseDeveloperExceptionPage();
      }
      else
      {
         app.UseExceptionHandler("/Home/Error");
         // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
         app.UseHsts();
      }

      app.UseStaticFiles();


      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
         endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
      });

      //The process's exit runs the composition's mirror: the client disconnects, the host disposes its endpoints - each
      //driving its own retract, stop-sending, stop-listening phases - and the registry closes after the retractions.
      app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
      {
         _client.DisposeAsync().AsTask().GetAwaiter().GetResult();
         _host.DisposeAsync().AsTask().GetAwaiter().GetResult();
         _endpointRegistry.Dispose();
      });
   }
}
