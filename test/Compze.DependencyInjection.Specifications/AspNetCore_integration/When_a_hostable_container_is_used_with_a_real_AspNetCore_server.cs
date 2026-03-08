using System.Net;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
#pragma warning disable CA2234

namespace Compze.DependencyInjection.Specifications.AspNetCore_integration;

interface ICompzeSingletonService
{
   string Id { get; }
}

class CompzeSingletonService : ICompzeSingletonService
{
   public string Id { get; } = Guid.NewGuid().ToString();
}

interface ICompzeScopedService
{
   string Id { get; }
}

class CompzeScopedService : ICompzeScopedService
{
   public string Id { get; } = Guid.NewGuid().ToString();
}

public class When_a_hostable_container_is_used_with_a_real_AspNetCore_server
{
   async Task<(WebApplication App, HttpClient Client)> StartServer()
   {
      var (container, hostable) = DependencyInjectionContainerFactory.CreateHostableContainer();

      container.Register(
         Singleton.For<ICompzeSingletonService>().CreatedBy(() => new CompzeSingletonService()),
         Scoped.For<ICompzeScopedService>().CreatedBy(() => new CompzeScopedService()));

      var builder = WebApplication.CreateBuilder();
      builder.WebHost.UseUrls("http://127.0.0.1:0");
      hostable.UseAsServiceProviderFor(builder.Host);

      var app = builder.Build();

      app.MapGet("/singleton", (ICompzeSingletonService svc) => svc.Id);
      app.MapGet("/scoped", (ICompzeScopedService svc) => svc.Id);

      app.MapGet("/scoped-twice", (ICompzeScopedService svc1, HttpContext httpContext) =>
      {
         var svc2 = httpContext.RequestServices.GetService(typeof(ICompzeScopedService)) as ICompzeScopedService;
         return $"{svc1.Id}|{svc2!.Id}";
      });

      await app.StartAsync();

      var url = app.Urls.First();
      var client = new HttpClient { BaseAddress = new Uri(url) };
      return (app, client);
   }

   public class singleton_services : When_a_hostable_container_is_used_with_a_real_AspNetCore_server
   {
      [HostableDependencyInjectionContainerMatrix] public async Task returns_same_instance_across_multiple_requests()
      {
         var (app, client) = await StartServer();
         await using(app)
         using(client)
         {
            var id1 = await client.GetStringAsync("/singleton");
            var id2 = await client.GetStringAsync("/singleton");
            id1.Must().Be(id2);
         }
      }

      [HostableDependencyInjectionContainerMatrix] public async Task returns_non_empty_value()
      {
         var (app, client) = await StartServer();
         await using(app)
         using(client)
         {
            var id = await client.GetStringAsync("/singleton");
            id.Length.Must().BeGreaterThan(0);
         }
      }
   }

   public class scoped_services : When_a_hostable_container_is_used_with_a_real_AspNetCore_server
   {
      [HostableDependencyInjectionContainerMatrix] public async Task returns_different_instances_across_requests()
      {
         var (app, client) = await StartServer();
         await using(app)
         using(client)
         {
            var id1 = await client.GetStringAsync("/scoped");
            var id2 = await client.GetStringAsync("/scoped");
            id1.Must().NotBe(id2);
         }
      }

      [HostableDependencyInjectionContainerMatrix] public async Task returns_same_instance_within_a_single_request()
      {
         var (app, client) = await StartServer();
         await using(app)
         using(client)
         {
            var response = await client.GetStringAsync("/scoped-twice");
            var parts = response.Split('|');
            parts[0].Must().Be(parts[1]);
         }
      }
   }

   public class aspnetcore_framework_services : When_a_hostable_container_is_used_with_a_real_AspNetCore_server
   {
      [HostableDependencyInjectionContainerMatrix] public async Task server_responds_to_http_requests()
      {
         var (app, client) = await StartServer();
         await using(app)
         using(client)
         {
            var response = await client.GetAsync("/singleton");
            response.StatusCode.Must().Be(HttpStatusCode.OK);
         }
      }

      [HostableDependencyInjectionContainerMatrix] public async Task non_existent_endpoint_returns_404()
      {
         var (app, client) = await StartServer();
         await using(app)
         using(client)
         {
            var response = await client.GetAsync("/nonexistent");
            response.StatusCode.Must().Be(HttpStatusCode.NotFound);
         }
      }
   }
}
