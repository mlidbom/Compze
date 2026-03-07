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
   public void Should_throw_when_singleton_depends_on_untracked_transient_service()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            UntrackedTransient.For<IUntrackedTransientService>().CreatedBy(() => new UntrackedTransientService()),
            Singleton.For<ISingletonService>().CreatedBy((IUntrackedTransientService transient) => new SingletonServiceDependingOnUntrackedTransient(transient))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("UntrackedTransient");
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

   interface IUntrackedTransientService;
   class UntrackedTransientService : IUntrackedTransientService;
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnUntrackedTransient(IUntrackedTransientService _) : ISingletonService;
#pragma warning restore CS9113 // Parameter is unread.

}
