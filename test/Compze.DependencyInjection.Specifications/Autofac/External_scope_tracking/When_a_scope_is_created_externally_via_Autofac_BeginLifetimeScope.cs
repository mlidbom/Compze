using Compze.DependencyInjection.Autofac;
using Compze.Must;
using Compze.xUnitBDD;
// ReSharper disable InconsistentNaming

namespace Compze.DependencyInjection.Specifications.Autofac.External_scope_tracking;

interface IScopedCounter
{
   int Value { get; }
}

class ScopedCounter : IScopedCounter
{
   static int _nextValue;
   public int Value { get; } = Interlocked.Increment(ref _nextValue);
}

interface ISingletonService;
class SingletonService : ISingletonService;

public class When_a_scope_is_created_externally_via_Autofac_BeginLifetimeScope
{
   static AutofacDependencyInjectionContainer CreateContainerWithScopedRegistration()
   {
      var container = new AutofacDependencyInjectionContainer();
      container.Register(Scoped.For<IScopedCounter>().CreatedBy(() => new ScopedCounter()));
      container.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));
      _ = container.ServiceLocator;
      return container;
   }

   public class resolving_scoped_services : When_a_scope_is_created_externally_via_Autofac_BeginLifetimeScope
   {
      [XF] public void scoped_service_is_resolvable_within_the_external_scope()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using var externalScope = autofacScope.BeginLifetimeScope();
         container.Resolve<IScopedCounter>().Must().NotBeNull();
      }

      [XF] public void scoped_service_resolved_twice_in_same_external_scope_returns_same_instance()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using var externalScope = autofacScope.BeginLifetimeScope();
         var first = container.Resolve<IScopedCounter>();
         var second = container.Resolve<IScopedCounter>();
         first.Must().Be(second);
      }

      [XF] public void different_external_scopes_resolve_different_scoped_instances()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         IScopedCounter fromScope1;
         using(autofacScope.BeginLifetimeScope())
         {
            fromScope1 = container.Resolve<IScopedCounter>();
         }

         using(autofacScope.BeginLifetimeScope())
         {
            var fromScope2 = container.Resolve<IScopedCounter>();
            fromScope2.Must().NotBe(fromScope1);
         }
      }

      [XF] public void after_external_scope_is_disposed_resolving_uses_root_scope()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using(autofacScope.BeginLifetimeScope())
         {
            container.Resolve<IScopedCounter>().Must().NotBeNull();
         }

         // After external scope is disposed, singletons should still resolve from root
         container.Resolve<ISingletonService>().Must().NotBeNull();
      }
   }

   public class with_nested_external_scopes : When_a_scope_is_created_externally_via_Autofac_BeginLifetimeScope
   {
      [XF] public void nested_external_scopes_each_have_their_own_scoped_instances()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using var outerScope = autofacScope.BeginLifetimeScope();
         var outerInstance = container.Resolve<IScopedCounter>();

         using var innerScope = outerScope.BeginLifetimeScope();
         var innerInstance = container.Resolve<IScopedCounter>();

         outerInstance.Must().NotBe(innerInstance);
      }

      [XF] public void disposing_inner_external_scope_restores_outer_scope()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using var outerScope = autofacScope.BeginLifetimeScope();
         var outerInstance = container.Resolve<IScopedCounter>();

         using(outerScope.BeginLifetimeScope())
         {
            container.Resolve<IScopedCounter>().Must().NotBe(outerInstance);
         }

         // After inner scope disposed, should resolve from outer scope again
         container.Resolve<IScopedCounter>().Must().Be(outerInstance);
      }
   }

   public class mixing_external_and_internal_scopes : When_a_scope_is_created_externally_via_Autofac_BeginLifetimeScope
   {
      [XF] public void internal_scope_inside_external_scope_works()
      {
         using var container = CreateContainerWithScopedRegistration();
         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using var externalScope = autofacScope.BeginLifetimeScope();
         var externalInstance = container.Resolve<IScopedCounter>();

         using(container.ServiceLocator.BeginScope())
         {
            var internalInstance = container.Resolve<IScopedCounter>();
            internalInstance.Must().NotBe(externalInstance);
         }

         // After internal scope disposed, back to the external scope
         container.Resolve<IScopedCounter>().Must().Be(externalInstance);
      }

      [XF] public void external_scope_inside_internal_scope_works()
      {
         using var container = CreateContainerWithScopedRegistration();
         var rootAutofacScope = ((IAutofacContainerInternals)container).LifetimeScope;

         using(container.ServiceLocator.BeginScope())
         {
            var internalInstance = container.Resolve<IScopedCounter>();

            // ASP.NET Core always creates scopes from the root IServiceScopeFactory
            using(rootAutofacScope.BeginLifetimeScope())
            {
               var externalInstance = container.Resolve<IScopedCounter>();
               externalInstance.Must().NotBe(internalInstance);
            }

            // Back to the internal scope
            container.Resolve<IScopedCounter>().Must().Be(internalInstance);
         }
      }
   }

   public class singleton_resolution : When_a_scope_is_created_externally_via_Autofac_BeginLifetimeScope
   {
      [XF] public void singletons_are_same_instance_in_external_scopes_as_in_root()
      {
         using var container = CreateContainerWithScopedRegistration();
         var rootSingleton = container.Resolve<ISingletonService>();

         var autofacScope = ((IAutofacContainerInternals)container).LifetimeScope;
         using var externalScope = autofacScope.BeginLifetimeScope();

         var scopedSingleton = container.Resolve<ISingletonService>();
         scopedSingleton.Must().Be(rootSingleton);
      }
   }
}
