using System.Net;
using Compze.DependencyInjection.Abstractions;
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

class HostableContainerTestServer : IAsyncDisposable
{
   readonly ILegacyContainer _container;
   readonly WebApplication _app;
   readonly HttpClient _client;

   HostableContainerTestServer(ILegacyContainer container, WebApplication app, HttpClient client)
   {
      _container = container;
      _app = app;
      _client = client;
   }

   public static async Task<HostableContainerTestServer> StartAsync()
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
      return new HostableContainerTestServer(container, app, client);
   }

   public Task<string> GetStringAsync(string requestUri) => _client.GetStringAsync(requestUri);
   public Task<HttpResponseMessage> GetAsync(string requestUri) => _client.GetAsync(requestUri);

   public async ValueTask DisposeAsync()
   {
      _client.Dispose();
      await _app.DisposeAsync();
      await _container.DisposeAsync();
   }
}

public class When_a_hostable_container_is_used_with_a_real_AspNetCore_server
{
   public class singleton_services : When_a_hostable_container_is_used_with_a_real_AspNetCore_server
   {
      [HostableDependencyInjectionContainerMatrix] public async Task returns_same_instance_across_multiple_requests()
      {
         await using var server = await HostableContainerTestServer.StartAsync();
         var id1 = await server.GetStringAsync("/singleton");
         var id2 = await server.GetStringAsync("/singleton");
         id1.Must().Be(id2);
      }

      [HostableDependencyInjectionContainerMatrix] public async Task returns_non_empty_value()
      {
         await using var server = await HostableContainerTestServer.StartAsync();
         var id = await server.GetStringAsync("/singleton");
         id.Length.Must().BeGreaterThan(0);
      }
   }

   public class scoped_services : When_a_hostable_container_is_used_with_a_real_AspNetCore_server
   {
      [HostableDependencyInjectionContainerMatrix] public async Task returns_different_instances_across_requests()
      {
         await using var server = await HostableContainerTestServer.StartAsync();
         var id1 = await server.GetStringAsync("/scoped");
         var id2 = await server.GetStringAsync("/scoped");
         id1.Must().NotBe(id2);
      }

      [HostableDependencyInjectionContainerMatrix] public async Task returns_same_instance_within_a_single_request()
      {
         await using var server = await HostableContainerTestServer.StartAsync();
         var response = await server.GetStringAsync("/scoped-twice");
         var parts = response.Split('|');
         parts[0].Must().Be(parts[1]);
      }
   }

   public class aspnetcore_framework_services : When_a_hostable_container_is_used_with_a_real_AspNetCore_server
   {
      [HostableDependencyInjectionContainerMatrix] public async Task server_responds_to_http_requests()
      {
         await using var server = await HostableContainerTestServer.StartAsync();
         var response = await server.GetAsync("/singleton");
         response.StatusCode.Must().Be(HttpStatusCode.OK);
      }

      [HostableDependencyInjectionContainerMatrix] public async Task non_existent_endpoint_returns_404()
      {
         await using var server = await HostableContainerTestServer.StartAsync();
         var response = await server.GetAsync("/nonexistent");
         response.StatusCode.Must().Be(HttpStatusCode.NotFound);
      }
   }
}
