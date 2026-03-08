using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

class CompzeMicrosoftServiceProvider(MicrosoftDependencyInjectionContainer compzeContainer, ServiceProvider innerProvider) : IServiceProvider, IServiceScopeFactory, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly MicrosoftDependencyInjectionContainer _compzeContainer = compzeContainer;
   readonly ServiceProvider _innerProvider = innerProvider;

   public object? GetService(Type serviceType)
   {
      if(serviceType == typeof(IServiceScopeFactory)) return this;
      if(serviceType == typeof(IServiceProvider)) return this;
      return _innerProvider.GetService(serviceType);
   }

   public object GetRequiredService(Type serviceType)
   {
      if(serviceType == typeof(IServiceScopeFactory)) return this;
      if(serviceType == typeof(IServiceProvider)) return this;
      return _innerProvider.GetRequiredService(serviceType);
   }

   public IServiceScope CreateScope()
   {
      var innerScope = _innerProvider.CreateScope();
      var internals = (IMicrosoftContainerInternals)_compzeContainer;
      internals.PushExternalScope(innerScope);
      return new CompzeMicrosoftServiceScope(this, innerScope, internals);
   }

   public void Dispose() => _innerProvider.Dispose();
   public ValueTask DisposeAsync() => _innerProvider.DisposeAsync();
}

class CompzeMicrosoftServiceScope(CompzeMicrosoftServiceProvider serviceProvider, IServiceScope innerScope, IMicrosoftContainerInternals compzeInternals) : IServiceScope
{
   readonly IServiceScope _innerScope = innerScope;
   readonly IMicrosoftContainerInternals _compzeInternals = compzeInternals;
   readonly CompzeMicrosoftServiceProvider _serviceProvider = serviceProvider;

   public IServiceProvider ServiceProvider => _serviceProvider;

   public void Dispose()
   {
      _compzeInternals.PopExternalScope();
      _innerScope.Dispose();
   }
}
