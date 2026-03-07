using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning;

public class When_cloning_a_container
{
   [DependencyInjectionContainerMatrix]
   public void the_clone_is_marked_as_a_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      source.IsClone.Must().BeFalse();

      using var clone = source.Clone();

      clone.IsClone.Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void the_source_is_not_marked_as_a_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone = source.Clone();

      source.IsClone.Must().BeFalse();
   }

   public class singleton_registrations : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void clone_resolves_a_different_singleton_instance_than_source()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

         using var clone = source.Clone();

         var sourceInstance = source.ServiceLocator.Resolve<ISingletonService>();
         var cloneInstance = clone.ServiceLocator.Resolve<ISingletonService>();

         sourceInstance.Must().NotBe(cloneInstance);
      }

      [DependencyInjectionContainerMatrix]
      public void clone_returns_same_instance_on_repeated_resolves()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

         using var clone = source.Clone();

         var first = clone.ServiceLocator.Resolve<ISingletonService>();
         var second = clone.ServiceLocator.Resolve<ISingletonService>();

         first.Must().Be(second);
      }

      [DependencyInjectionContainerMatrix]
      public void delegated_singleton_resolves_to_same_instance_as_source()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(
            Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()).DelegateToParentServiceLocatorWhenCloning()
         );

         var sourceInstance = source.ServiceLocator.Resolve<ISingletonService>();

         using var clone = source.Clone();

         var cloneInstance = clone.ServiceLocator.Resolve<ISingletonService>();

         sourceInstance.Must().Be(cloneInstance);
      }
   }

   public class scoped_services : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void clone_can_resolve_scoped_services_within_a_scope()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

         using var clone = source.Clone();
         var serviceLocator = clone.ServiceLocator;

         using(serviceLocator.BeginScope())
         {
            serviceLocator.Resolve<IScopedService>().Must().NotBeNull();
         }
      }

      [DependencyInjectionContainerMatrix]
      public void scoped_instances_are_independent_between_source_and_clone()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

         using var clone = source.Clone();

         IScopedService sourceInstance;
         IScopedService cloneInstance;

         using(source.ServiceLocator.BeginScope())
         {
            sourceInstance = source.ServiceLocator.Resolve<IScopedService>();
         }

         using(clone.ServiceLocator.BeginScope())
         {
            cloneInstance = clone.ServiceLocator.Resolve<IScopedService>();
         }

         sourceInstance.Must().NotBe(cloneInstance);
      }
   }

   public class tracked_transients : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void clone_can_resolve_tracked_transient_services()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()));

         using var clone = source.Clone();

         var first = clone.ServiceLocator.Resolve<ITransientService>();
         var second = clone.ServiceLocator.Resolve<ITransientService>();

         first.Must().NotBe(second);
      }

      [DependencyInjectionContainerMatrix]
      public void clone_disposes_its_own_tracked_transients_independently()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

         var clone = source.Clone();
         var cloneInstance = (DisposableService)clone.ServiceLocator.Resolve<IDisposableService>();

         cloneInstance.IsDisposed.Must().BeFalse();
         clone.Dispose();
         cloneInstance.IsDisposed.Must().BeTrue();
      }
   }

   public class untracked_transients : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void clone_can_resolve_untracked_transient_services()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(Transient.For<ITransientService>().CreatedBy(() => new TransientService()));

         using var clone = source.Clone();

         var first = clone.ServiceLocator.Resolve<ITransientService>();
         var second = clone.ServiceLocator.Resolve<ITransientService>();

         first.Must().NotBe(second);
      }
   }

   public class lifestyle_validation : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void clone_preserves_AllowSingletonDependent_flag()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(
            TrackedTransient.For<ITransientService>().AllowSingletonDependent().CreatedBy(() => new TransientService()),
            Singleton.For<ISingletonService>().CreatedBy((ITransientService t) => new SingletonServiceDependingOnTransient(t))
         );

         using var clone = source.Clone();
         clone.ServiceLocator.Resolve<ISingletonService>().Must().NotBeNull();
      }

      [DependencyInjectionContainerMatrix]
      public void clone_preserves_AllowScopedDependent_flag()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(
            TrackedTransient.For<ITransientService>().AllowScopedDependent().CreatedBy(() => new TransientService()),
            Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTransient(t))
         );

         using var clone = source.Clone();
         using(clone.ServiceLocator.BeginScope())
         {
            clone.ServiceLocator.Resolve<IScopedService>().Must().NotBeNull();
         }
      }
   }

   public class cross_lifestyle_dependencies : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void clone_resolves_services_with_dependencies()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(
            Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()),
            Scoped.For<IScopedService>().CreatedBy((ISingletonService s) => new ScopedServiceDependingOnSingleton(s))
         );

         using var clone = source.Clone();
         using(clone.ServiceLocator.BeginScope())
         {
            clone.ServiceLocator.Resolve<IScopedService>().Must().NotBeNull();
         }
      }
   }

   public class multiple_clones : When_cloning_a_container
   {
      [DependencyInjectionContainerMatrix]
      public void source_can_be_cloned_multiple_times()
      {
         using var source = DependencyInjectionContainerFactory.CreateContainer();
         source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

         using var clone1 = source.Clone();
         using var clone2 = source.Clone();

         var instance1 = clone1.ServiceLocator.Resolve<ISingletonService>();
         var instance2 = clone2.ServiceLocator.Resolve<ISingletonService>();

         instance1.Must().NotBe(instance2);
      }
   }

   interface ISingletonService;
   class SingletonService : ISingletonService;

   interface IScopedService;
   class ScopedService : IScopedService;

   interface ITransientService;
   class TransientService : ITransientService;

   interface IDisposableService;
   class DisposableService : IDisposableService, IDisposable
   {
      public bool IsDisposed { get; private set; }
      public void Dispose() => IsDisposed = true;
   }

#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnTransient(ITransientService _) : ISingletonService;
   class ScopedServiceDependingOnTransient(ITransientService _) : IScopedService;
   class ScopedServiceDependingOnSingleton(ISingletonService _) : IScopedService;
#pragma warning restore CS9113 // Parameter is unread.
}
