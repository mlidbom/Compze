using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Wiring;
using JetBrains.Annotations;
using Compze.Typermedia;

namespace AccountManagement.UI.MVC;

public class Startup
{
   readonly IEndpointHost _host;
   readonly IEndpoint _endpoint;
   TestClient _client = null!;

   public Startup(IConfiguration configuration)
   {
      Configuration = configuration;
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents());
      _endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(_host);
   }

   // ReSharper disable once MemberCanBePrivate.Global
   public IConfiguration Configuration { [UsedImplicitly] get; }

   // This method gets called by the runtime. Use this method to add services to the container.
   [UsedImplicitly] public void ConfigureServices(IServiceCollection services)
   {
      services.AddMvc();

      _host.Start();

      _client = TestClient.ConnectTo(_endpoint.Address!).GetAwaiter().GetResult();
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