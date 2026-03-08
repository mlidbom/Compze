using System.Net;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.Must;
using Compze.xUnitBDD;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Compze.DependencyInjection.Specifications.Autofac.AspNetCore_integration;

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

public class When_CompzeAutofacServiceProviderFactory_is_used_with_a_real_AspNetCore_server : IAsyncLifetime
{
   WebApplication? _app;
   HttpClient? _client;
   readonly AutofacDependencyInjectionContainer _compzeContainer = new();

   public async ValueTask InitializeAsync()
   {
      _compzeContainer.Register(
         Singleton.For<ICompzeSingletonService>().CreatedBy(() => new CompzeSingletonService()),
         Scoped.For<ICompzeScopedService>().CreatedBy(() => new CompzeScopedService()));

      var builder = WebApplication.CreateBuilder();
      builder.WebHost.UseUrls("http://127.0.0.1:0");
      new HostableAutofacContainer(_compzeContainer).UseAsServiceProviderFor(builder.Host);

      _app = builder.Build();

      _app.MapGet("/singleton", (ICompzeSingletonService svc) => svc.Id);
      _app.MapGet("/scoped", (ICompzeScopedService svc) => svc.Id);

      _app.MapGet("/scoped-twice", (ICompzeScopedService svc1, HttpContext httpContext) =>
      {
         var svc2 = httpContext.RequestServices.GetService(typeof(ICompzeScopedService)) as ICompzeScopedService;
         return $"{svc1.Id}|{svc2!.Id}";
      });

      await _app.StartAsync();

      var url = _app.Urls.First();
      _client = new HttpClient { BaseAddress = new Uri(url) };
   }

   public async ValueTask DisposeAsync()
   {
      _client?.Dispose();
      if(_app != null) await _app.DisposeAsync();
      await _compzeContainer.DisposeAsync();
   }

   public class singleton_services : When_CompzeAutofacServiceProviderFactory_is_used_with_a_real_AspNetCore_server
   {
      [XF] public async Task returns_same_instance_across_multiple_requests()
      {
         var id1 = await _client!.GetStringAsync("/singleton");
         var id2 = await _client!.GetStringAsync("/singleton");
         id1.Must().Be(id2);
      }

      [XF] public async Task returns_non_empty_value()
      {
         var id = await _client!.GetStringAsync("/singleton");
         id.Length.Must().BeGreaterThan(0);
      }
   }

   public class scoped_services : When_CompzeAutofacServiceProviderFactory_is_used_with_a_real_AspNetCore_server
   {
      [XF] public async Task returns_different_instances_across_requests()
      {
         var id1 = await _client!.GetStringAsync("/scoped");
         var id2 = await _client!.GetStringAsync("/scoped");
         id1.Must().NotBe(id2);
      }

      [XF] public async Task returns_same_instance_within_a_single_request()
      {
         var response = await _client!.GetStringAsync("/scoped-twice");
         var parts = response.Split('|');
         parts[0].Must().Be(parts[1]);
      }
   }

   public class aspnetcore_framework_services : When_CompzeAutofacServiceProviderFactory_is_used_with_a_real_AspNetCore_server
   {
      [XF] public async Task server_responds_to_http_requests()
      {
         var response = await _client!.GetAsync("/singleton");
         response.StatusCode.Must().Be(HttpStatusCode.OK);
      }

      [XF] public async Task non_existent_endpoint_returns_404()
      {
         var response = await _client!.GetAsync("/nonexistent");
         response.StatusCode.Must().Be(HttpStatusCode.NotFound);
      }
   }
}
