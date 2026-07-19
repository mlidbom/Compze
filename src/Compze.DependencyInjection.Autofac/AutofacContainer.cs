using Autofac;
using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Runtime.Resolution.Internal;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using ContainerBuilder = Compze.DependencyInjection.Wiring.ContainerBuilder;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacContainer : DependencyInjectionContainer, IRootResolver, IScopeFactory, IAutofacContainerInternals
{
   readonly IContainer _container;

   internal AutofacContainer(IContainer container, IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
      : base(registrations, sourceRegistrar) =>
      _container = container;

   public override AutofacContainerBuilder CreateCloneContainerBuilder() => (AutofacContainerBuilder)base.CreateCloneContainerBuilder();

   internal override AutofacContainerBuilder CreateChildContainerBuilder() => (AutofacContainerBuilder)base.CreateChildContainerBuilder();

   protected override object ResolveCore(Type serviceType) => _container.Resolve(serviceType);

   protected override IEnumerable<object> ResolveSetCore(Type serviceType) =>
      (IEnumerable<object>)_container.Resolve(typeof(IEnumerable<>).MakeGenericType(serviceType));

   public IScope BeginScope()
   {
      var lifetimeScope = _container.BeginLifetimeScope();
      var scopeResolver = lifetimeScope.Resolve<ScopeResolver>();
      return new Scope(scopeResolver, lifetimeScope);
   }

   IContainer IAutofacContainerInternals.Container => _container;

   public override void Dispose() => _container.Dispose();

   public override async ValueTask DisposeAsync() => await _container.DisposeAsync().caf();

   protected override ContainerBuilder CreateConcreteBuilder(IComponentRegistrar registrar) => new AutofacContainerBuilder(registrar);

   sealed class Scope(IScopeResolver scopeResolver, ILifetimeScope lifetimeScope) : IScope
   {
      readonly ILifetimeScope _lifetimeScope = lifetimeScope;
      public IScopeResolver Resolver { get; } = scopeResolver;

      public void Dispose() => _lifetimeScope.Dispose();
   }
}
