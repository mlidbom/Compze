using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using static Compze.Must.MustActions;

namespace Compze.DependencyInjection.Specifications;

public class LifestyleValidationTests
{
   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_singleton_depends_on_scoped_service()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         builder.Registrar.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            Singleton.For<ISingletonService>().CreatedBy((IScopedService scoped) => new SingletonServiceDependingOnScoped(scoped))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Scoped");
   }


   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_singleton_depends_on_transient_service()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         builder.Registrar.Register(
            TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()),
            Singleton.For<ISingletonService>().CreatedBy((ITransientService transient) => new SingletonServiceDependingOnTransient(transient))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Transient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_singleton_depending_on_transient_with_AllowSingletonDependent()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         TrackedTransient.For<ITransientService>().AllowSingletonDependent().CreatedBy(() => new TransientService()),
         Singleton.For<ISingletonService>().CreatedBy((ITransientService t) => new SingletonServiceDependingOnTransient(t))
      );

      using var container = builder.Build();
      container.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_scoped_depends_on_transient()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         builder.Registrar.Register(
            TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()),
            Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTransient(t))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Scoped");
      exception.Message.Must().Contain("Transient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_scoped_depending_on_transient_with_AllowScopedDependent()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         TrackedTransient.For<ITransientService>().AllowScopedDependent().CreatedBy(() => new TransientService()),
         Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTransient(t))
      );

      using var container = builder.Build();
      using var scope = container.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_singleton_depends_on_a_service_resolver_of_a_transient()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         builder.Registrar.Register(
            TrackedTransient.For<ITransientService>().WithServiceResolver().CreatedBy(() => new TransientService()),
            Singleton.For<ISingletonService>().CreatedBy((IServiceResolver<ITransientService> transientResolver) => new SingletonServiceDependingOnTransientResolver(transientResolver))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Transient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_singleton_depending_on_a_service_resolver_of_a_transient_with_AllowSingletonDependent()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         TrackedTransient.For<ITransientService>().AllowSingletonDependent().WithServiceResolver().CreatedBy(() => new TransientService()),
         Singleton.For<ISingletonService>().CreatedBy((IServiceResolver<ITransientService> transientResolver) => new SingletonServiceDependingOnTransientResolver(transientResolver))
      );

      using var container = builder.Build();
      container.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_scoped_depending_on_a_service_resolver_of_a_transient_with_AllowScopedDependent()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         TrackedTransient.For<ITransientService>().AllowScopedDependent().WithServiceResolver().CreatedBy(() => new TransientService()),
         Scoped.For<IScopedService>().CreatedBy((IServiceResolver<ITransientService> transientResolver) => new ScopedServiceDependingOnTransientResolver(transientResolver))
      );

      using var container = builder.Build();
      using var scope = container.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_transient_depending_on_scoped_service()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
         TrackedTransient.For<ITransientService>().CreatedBy((IScopedService scoped) => new TransientDependingOnScoped(scoped))
      );

      using var container = builder.Build();
      using var scope = container.BeginScope();
      scope.Resolve<ITransientService>().Must().NotBeNull();
   }

   interface IScopedService;
   class ScopedService : IScopedService;

   interface ISingletonService;
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnScoped(IScopedService _) : ISingletonService;
#pragma warning restore CS9113 // Parameter is unread.

   interface ITransientService;
   class TransientService : ITransientService;
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnTransient(ITransientService _) : ISingletonService;
   class ScopedServiceDependingOnTransient(ITransientService _) : IScopedService;
   class TransientDependingOnScoped(IScopedService _) : ITransientService;
   class SingletonServiceDependingOnTransientResolver(IServiceResolver<ITransientService> _) : ISingletonService;
   class ScopedServiceDependingOnTransientResolver(IServiceResolver<ITransientService> _) : IScopedService;
#pragma warning restore CS9113 // Parameter is unread.

}
