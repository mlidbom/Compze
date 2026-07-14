using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;
using static Compze.Must.MustActions;

namespace Compze.DependencyInjection.Specifications;

public class ScopeValidationTests
{
   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_resolving_scoped_service_from_root()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IScopedService>().CreatedBy(() => new ScopedService())
      );

      using var container = builder.Build();

      Invoking(() => container.Resolve<IScopedService>())
         .Must().Throw<Exception>();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_succeed_resolving_scoped_service_from_root_when_opted_out()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IScopedService>().CreatedBy(() => new ScopedService())
      );

      using var container = builder.Build(new ContainerOptions { AllowScopedResolutionFromRoot = true });

      container.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_resolve_scoped_service_within_scope_normally()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IScopedService>().CreatedBy(() => new ScopedService())
      );

      using var container = builder.Build();
      using var scope = container.BeginScope();

      scope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_singleton_resolution_from_root_regardless_of_scope_validation()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService())
      );

      using var container = builder.Build();

      container.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_transient_resolution_from_root_regardless_of_scope_validation()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         TrackedTransient.For<ISingletonService>().CreatedBy(() => new SingletonService())
      );

      using var container = builder.Build();

      container.Resolve<ISingletonService>().Must().NotBeNull();
   }

   interface IScopedService;
   class ScopedService : IScopedService;

   interface ISingletonService;
   class SingletonService : ISingletonService;
}
