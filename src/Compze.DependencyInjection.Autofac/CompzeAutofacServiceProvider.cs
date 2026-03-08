using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Autofac;

class CompzeAutofacServiceProvider(AutofacDependencyInjectionContainer container) : IServiceProvider, IServiceScopeFactory, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly AutofacDependencyInjectionContainer _container = container;

   public object? GetService(Type serviceType)
   {
      try { return _container.Resolve(serviceType); }
      catch { return null; }
   }

   public object GetRequiredService(Type serviceType) => _container.Resolve(serviceType);

   public IServiceScope CreateScope()
   {
      var rootScope = ((IAutofacContainerInternals)_container).LifetimeScope;
      var childScope = rootScope.BeginLifetimeScope();
      return new CompzeAutofacServiceScope(this, childScope);
   }

   public void Dispose() => _container.Dispose();
   public ValueTask DisposeAsync() => _container.DisposeAsync();
}

class CompzeAutofacServiceScope(IServiceProvider serviceProvider, ILifetimeScope scope) : IServiceScope
{
   readonly ILifetimeScope _scope = scope;
   public IServiceProvider ServiceProvider { get; } = serviceProvider;

   public void Dispose() => _scope.Dispose();
}
