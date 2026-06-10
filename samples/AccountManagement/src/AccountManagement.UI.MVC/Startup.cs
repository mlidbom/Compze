using Compze.Abstractions.Hosting.Public;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Typermedia.Client;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Typermedia.Hosting.Testing;
using Compze.Typermedia.Hosting.Testing.Wiring;
using Compze.Underscore;
using JetBrains.Annotations;

namespace AccountManagement.UI.MVC;

public class Startup
{
   readonly IEndpointHost _host;
   readonly IEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Startup(IConfiguration configuration)
   {
      Configuration = configuration;
      //This demo host runs on the testing wiring: each endpoint gets both paradigm transports and the full SQL persistence stack against a throwaway pooled database.
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                          ._mutate(it => it.Registrar
                                                                           .CurrentTestsTessagingTransport()
                                                                           .CurrentTestsTypermediaTransport()
                                                                           .CurrentTestsConfiguredSqlLayer(connectionStringName: Guid.NewGuid().ToString())));
      _endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(_host);
   }

   // ReSharper disable once MemberCanBePrivate.Global
   public IConfiguration Configuration { [UsedImplicitly] get; }

   // This method gets called by the runtime. Use this method to add services to the container.
   [UsedImplicitly] public void ConfigureServices(IServiceCollection services)
   {
      services.AddMvc();

      _host.Start();

      _client = TypermediaTestClient.ConnectTo(_endpoint.TypermediaAddress!, mapper => mapper.RegisterAccountManagementTypeMappings()).GetAwaiter().GetResult();
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
   }
}
