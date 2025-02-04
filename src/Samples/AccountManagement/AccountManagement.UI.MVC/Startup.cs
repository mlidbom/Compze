﻿using AccountManagement.API;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccountManagement.UI.MVC;

public class Startup
{
   readonly IEndpointHost _host;
   readonly IEndpoint _clientEndpoint;

   public Startup(IConfiguration configuration)
   {
      Configuration = configuration;
      _host = EndpointHost.Production.Create(TestingContainerFactory.Create);
      _clientEndpoint = _host.RegisterClientEndpoint(AccountApi.RegisterWithClientEndpoint);

   }

   // ReSharper disable once MemberCanBePrivate.Global
   public IConfiguration Configuration { [UsedImplicitly] get; }

   // This method gets called by the runtime. Use this method to add services to the container.
   [UsedImplicitly] public void ConfigureServices(IServiceCollection services)
   {
      services.AddMvc();

      _host.Start();
      services.AddScoped(_ => _clientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>());
   }

   // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
   [UsedImplicitly] public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
      if (env.IsDevelopment())
      {
         app.UseDeveloperExceptionPage();
         app.UseBrowserLink();
      }
      else
      {
         app.UseExceptionHandler("/Home/Error");
         // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
         app.UseHsts();
      }

      app.UseStaticFiles();


      app.UseRouting();

      app.Use(async (_, next) => await _clientEndpoint.ExecuteClientRequestAsync(async () => await next.Invoke().CaF()).CaF());

      app.UseEndpoints(endpoints =>
      {
         endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
      });
   }
}