using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftContainer : DependencyInjectionContainer, IRootResolver, IScopeFactory, IMicrosoftContainerInternals
{
   readonly ServiceProvider _serviceProvider;
   bool _isDisposed;

   internal MicrosoftContainer(ServiceProvider serviceProvider, IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
      : base(registrations, sourceRegistrar) =>
      _serviceProvider = serviceProvider;

   public override MicrosoftContainerBuilder CreateCloneContainerBuilder() => (MicrosoftContainerBuilder)base.CreateCloneContainerBuilder();

   public override MicrosoftContainerBuilder CreateChildContainerBuilder() => (MicrosoftContainerBuilder)base.CreateChildContainerBuilder();

   public object Resolve(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _serviceProvider.GetRequiredService(serviceType);
   }

   public IScope BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this);

      var scope = _serviceProvider.CreateAsyncScope();
      var scopeResolver = scope.ServiceProvider.GetRequiredService<ScopeResolver>();

      return new Scope(scopeResolver, () => scope.DisposeAsync().AsTask().GetAwaiter().GetResult());
   }

   IServiceProvider IMicrosoftContainerInternals.ServiceProvider => _serviceProvider;

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         _serviceProvider.DisposeAsync().AsTask().GetAwaiter().GetResult();
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         await _serviceProvider.DisposeAsync().caf();
      }
   }

   protected override ContainerBuilder CreateConcreteBuilder(IComponentRegistrar registrar) => new MicrosoftContainerBuilder(registrar);

   sealed class Scope(IScopeResolver scopeResolver, Action onDispose) : IScope
   {
      readonly Action _onDispose = onDispose;
      public IScopeResolver Resolver { get; } = scopeResolver;

      public void Dispose() => _onDispose();
   }
}
