using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.DependencyInjection;
using Compze.Must;
using static Compze.Must.MustActions;

namespace Compze.DependencyInjection.Specifications;

public class LifestyleValidationTests
{
   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_singleton_depends_on_scoped_service()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            Singleton.For<ISingletonService>().CreatedBy((IScopedService scoped) => new SingletonServiceDependingOnScoped(scoped))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Scoped");
   }


   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_singleton_depends_on_tracked_transient_service()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()),
            Singleton.For<ISingletonService>().CreatedBy((ITransientService transient) => new SingletonServiceDependingOnTransient(transient))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("TrackedTransient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_singleton_depends_on_transient_service()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            Transient.For<ITransientService2>().CreatedBy(() => new TransientService2()),
            Singleton.For<ISingletonService>().CreatedBy((ITransientService2 transient) => new SingletonServiceDependingOnTransient2(transient))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Transient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_singleton_depending_on_tracked_transient_with_AllowSingletonDependent()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         TrackedTransient.For<ITransientService>().AllowSingletonDependent().CreatedBy(() => new TransientService()),
         Singleton.For<ISingletonService>().CreatedBy((ITransientService t) => new SingletonServiceDependingOnTransient(t))
      );

      var serviceLocator = container.ServiceLocator;
      serviceLocator.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_singleton_depending_on_transient_with_AllowSingletonDependent()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Transient.For<ITransientService2>().AllowSingletonDependent().CreatedBy(() => new TransientService2()),
         Singleton.For<ISingletonService>().CreatedBy((ITransientService2 t) => new SingletonServiceDependingOnTransient2(t))
      );

      var serviceLocator = container.ServiceLocator;
      serviceLocator.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_scoped_depends_on_tracked_transient()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()),
            Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTrackedTransient(t))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Scoped");
      exception.Message.Must().Contain("TrackedTransient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_scoped_depends_on_transient()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            Transient.For<ITransientService2>().CreatedBy(() => new TransientService2()),
            Scoped.For<IScopedService>().CreatedBy((ITransientService2 t) => new ScopedServiceDependingOnTransient2(t))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Scoped");
      exception.Message.Must().Contain("Transient");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_scoped_depending_on_tracked_transient_with_AllowScopedDependent()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         TrackedTransient.For<ITransientService>().AllowScopedDependent().CreatedBy(() => new TransientService()),
         Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTrackedTransient(t))
      );

      var serviceLocator = container.ServiceLocator;
      using(serviceLocator.BeginScope())
      {
         serviceLocator.Resolve<IScopedService>().Must().NotBeNull();
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Should_allow_scoped_depending_on_transient_with_AllowScopedDependent()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Transient.For<ITransientService2>().AllowScopedDependent().CreatedBy(() => new TransientService2()),
         Scoped.For<IScopedService>().CreatedBy((ITransientService2 t) => new ScopedServiceDependingOnTransient2(t))
      );

      var serviceLocator = container.ServiceLocator;
      using(serviceLocator.BeginScope())
      {
         serviceLocator.Resolve<IScopedService>().Must().NotBeNull();
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_tracked_transient_depends_on_scoped_service()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            TrackedTransient.For<ITransientService>().CreatedBy((IScopedService scoped) => new TrackedTransientDependingOnScoped(scoped))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("TrackedTransient");
      exception.Message.Must().Contain("Scoped");
   }

   [DependencyInjectionContainerMatrix]
   public void Should_throw_when_transient_depends_on_scoped_service()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            Transient.For<ITransientService2>().CreatedBy((IScopedService scoped) => new TransientDependingOnScoped(scoped))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Transient");
      exception.Message.Must().Contain("Scoped");
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
#pragma warning restore CS9113 // Parameter is unread.

   interface ITransientService2;
   class TransientService2 : ITransientService2;
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnTransient2(ITransientService2 _) : ISingletonService;
   class ScopedServiceDependingOnTrackedTransient(ITransientService _) : IScopedService;
   class ScopedServiceDependingOnTransient2(ITransientService2 _) : IScopedService;
   class TrackedTransientDependingOnScoped(IScopedService _) : ITransientService;
   class TransientDependingOnScoped(IScopedService _) : ITransientService2;
#pragma warning restore CS9113 // Parameter is unread.

}
